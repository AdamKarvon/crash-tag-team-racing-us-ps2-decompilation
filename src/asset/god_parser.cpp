#include "god_parser.h"
#include <fstream>
#include <sstream>
#include <iostream>
#include <cctype>
#include <algorithm>

namespace CTTR {
namespace Asset {

    void GodObject::DispatchMethod(const std::string& methodName, const std::vector<GodValue>& args) {
        if (methodName == "SetPosition" && args.size() >= 3) {
            double x = 0.0, y = 0.0, z = 0.0;
            if (std::holds_alternative<double>(args[0])) x = std::get<double>(args[0]);
            else if (std::holds_alternative<int64_t>(args[0])) x = static_cast<double>(std::get<int64_t>(args[0]));

            if (std::holds_alternative<double>(args[1])) y = std::get<double>(args[1]);
            else if (std::holds_alternative<int64_t>(args[1])) y = static_cast<double>(std::get<int64_t>(args[1]));

            if (std::holds_alternative<double>(args[2])) z = std::get<double>(args[2]);
            else if (std::holds_alternative<int64_t>(args[2])) z = static_cast<double>(std::get<int64_t>(args[2]));

            m_position = glm::vec3(static_cast<float>(x), static_cast<float>(y), static_cast<float>(z));
            m_properties["Position"] = m_position;
        } else if (methodName == "SetQuaternion" && args.size() >= 4) {
            double x = 0.0, y = 0.0, z = 0.0, w = 0.0;
            if (std::holds_alternative<double>(args[0])) x = std::get<double>(args[0]);
            else if (std::holds_alternative<int64_t>(args[0])) x = static_cast<double>(std::get<int64_t>(args[0]));

            if (std::holds_alternative<double>(args[1])) y = std::get<double>(args[1]);
            else if (std::holds_alternative<int64_t>(args[1])) y = static_cast<double>(std::get<int64_t>(args[1]));

            if (std::holds_alternative<double>(args[2])) z = std::get<double>(args[2]);
            else if (std::holds_alternative<int64_t>(args[2])) z = static_cast<double>(std::get<int64_t>(args[2]));

            if (std::holds_alternative<double>(args[3])) w = std::get<double>(args[3]);
            else if (std::holds_alternative<int64_t>(args[3])) w = static_cast<double>(std::get<int64_t>(args[3]));

            m_quaternion = glm::vec4(static_cast<float>(x), static_cast<float>(y), static_cast<float>(z), static_cast<float>(w));
            m_properties["Quaternion"] = m_quaternion;
        } else if ((methodName == "SetLoadSet" || methodName == "SetLoadSetName") && !args.empty()) {
            if (std::holds_alternative<std::string>(args[0])) {
                m_loadSetName = std::get<std::string>(args[0]);
                m_properties["LoadSetName"] = m_loadSetName;
            }
        } else if (methodName == "Enable" && !args.empty()) {
            if (std::holds_alternative<bool>(args[0])) {
                m_enabled = std::get<bool>(args[0]);
                m_properties["Enable"] = m_enabled;
            }
        } else {
            if (args.size() == 1) {
                m_properties[methodName] = args[0];
            } else if (args.size() == 3 && std::holds_alternative<double>(args[0])) {
                double x = std::holds_alternative<double>(args[0]) ? std::get<double>(args[0]) : (double)std::get<int64_t>(args[0]);
                double y = std::holds_alternative<double>(args[1]) ? std::get<double>(args[1]) : (double)std::get<int64_t>(args[1]);
                double z = std::holds_alternative<double>(args[2]) ? std::get<double>(args[2]) : (double)std::get<int64_t>(args[2]);
                m_properties[methodName] = glm::vec3((float)x, (float)y, (float)z);
            }
        }
    }

    std::string GodParser::ExtractTheme(const std::string& levelName) {
        std::string theme;
        for (char c : levelName) {
            if (!std::isdigit(static_cast<unsigned char>(c))) {
                theme += static_cast<char>(std::tolower(static_cast<unsigned char>(c)));
            }
        }
        // Normalize any special suffixes
        if (theme == "fairys") theme = "fairy";
        if (theme == "solars") theme = "solar";
        if (theme == "dinoa") theme = "dino";
        if (theme == "egypta") theme = "egypt";
        if (theme == "adventa") theme = "adventure";
        if (theme == "advent") theme = "adventure";
        return theme;
    }

    namespace {

        enum class TokenType {
            EndOfFile,
            Identifier,
            StringLiteral,
            IntegerLiteral,
            FloatLiteral,
            BooleanLiteral,
            Dot,
            Comma,
            OpenParen,
            CloseParen,
            Assign,
            Colon
        };

        struct Token {
            TokenType type;
            std::string text;
            int64_t intValue = 0;
            double floatValue = 0.0;
            bool boolValue = false;
            size_t line = 1;
        };

        class Lexer {
        public:
            Lexer(const std::string& source) : m_src(source), m_pos(0), m_line(1) {}

            Token NextToken() {
                SkipWhitespaceAndComments();

                if (m_pos >= m_src.size()) {
                    return {TokenType::EndOfFile, "", 0, 0.0, false, m_line};
                }

                char c = m_src[m_pos];

                if (c == '(') { m_pos++; return {TokenType::OpenParen, "(", 0, 0.0, false, m_line}; }
                if (c == ')') { m_pos++; return {TokenType::CloseParen, ")", 0, 0.0, false, m_line}; }
                if (c == '.') { 
                    // Check for number like .5
                    if (m_pos + 1 < m_src.size() && std::isdigit(static_cast<unsigned char>(m_src[m_pos + 1]))) {
                        return ReadNumber();
                    }
                    m_pos++; 
                    return {TokenType::Dot, ".", 0, 0.0, false, m_line}; 
                }
                if (c == ',') { m_pos++; return {TokenType::Comma, ",", 0, 0.0, false, m_line}; }
                if (c == '=') { m_pos++; return {TokenType::Assign, "=", 0, 0.0, false, m_line}; }
                if (c == ':') { m_pos++; return {TokenType::Colon, ":", 0, 0.0, false, m_line}; }

                if (c == '"' || c == '\'') {
                    return ReadString(c);
                }

                if (std::isdigit(static_cast<unsigned char>(c)) || (c == '-' && m_pos + 1 < m_src.size() && (std::isdigit(static_cast<unsigned char>(m_src[m_pos + 1])) || m_src[m_pos + 1] == '.'))) {
                    return ReadNumber();
                }

                if (std::isalpha(static_cast<unsigned char>(c)) || c == '_' || c == '$' || c == '/') {
                    return ReadIdentifier();
                }

                // Any other character, consume and return as single char identifier
                m_pos++;
                return {TokenType::Identifier, std::string(1, c), 0, 0.0, false, m_line};
            }

        private:
            void SkipWhitespaceAndComments() {
                while (m_pos < m_src.size()) {
                    char c = m_src[m_pos];
                    if (c == '\n') {
                        m_line++;
                        m_pos++;
                    } else if (std::isspace(static_cast<unsigned char>(c))) {
                        m_pos++;
                    } else if (c == '-' && m_pos + 1 < m_src.size() && m_src[m_pos + 1] == '-') {
                        // Lua comment --
                        m_pos += 2;
                        if (m_pos + 1 < m_src.size() && m_src[m_pos] == '[' && m_src[m_pos + 1] == '[') {
                            // Block comment --[[ ... ]]
                            m_pos += 2;
                            while (m_pos + 1 < m_src.size() && !(m_src[m_pos] == ']' && m_src[m_pos + 1] == ']')) {
                                if (m_src[m_pos] == '\n') m_line++;
                                m_pos++;
                            }
                            if (m_pos + 1 < m_src.size()) m_pos += 2;
                        } else {
                            while (m_pos < m_src.size() && m_src[m_pos] != '\n') {
                                m_pos++;
                            }
                        }
                    } else if (c == '/' && m_pos + 1 < m_src.size() && m_src[m_pos + 1] == '/') {
                        // C++ comment //
                        m_pos += 2;
                        while (m_pos < m_src.size() && m_src[m_pos] != '\n') {
                            m_pos++;
                        }
                    } else if (c == '/' && m_pos + 1 < m_src.size() && m_src[m_pos + 1] == '*') {
                        // C block comment /* ... */
                        m_pos += 2;
                        while (m_pos + 1 < m_src.size() && !(m_src[m_pos] == '*' && m_src[m_pos + 1] == '/')) {
                            if (m_src[m_pos] == '\n') m_line++;
                            m_pos++;
                        }
                        if (m_pos + 1 < m_src.size()) m_pos += 2;
                    } else if (c == '#') {
                        // Shell comment #
                        while (m_pos < m_src.size() && m_src[m_pos] != '\n') {
                            m_pos++;
                        }
                    } else {
                        break;
                    }
                }
            }

            Token ReadString(char quote) {
                m_pos++; // skip open quote
                std::string str;
                while (m_pos < m_src.size()) {
                    char c = m_src[m_pos];
                    if (c == quote) {
                        m_pos++;
                        break;
                    } else if (c == '\\' && m_pos + 1 < m_src.size()) {
                        m_pos++;
                        char next = m_src[m_pos];
                        if (next == 'n') str += '\n';
                        else if (next == 't') str += '\t';
                        else if (next == 'r') str += '\r';
                        else if (next == '\\') str += '\\';
                        else if (next == '"') str += '"';
                        else if (next == '\'') str += '\'';
                        else str += next;
                        m_pos++;
                    } else {
                        if (c == '\n') m_line++;
                        str += c;
                        m_pos++;
                    }
                }
                return {TokenType::StringLiteral, str, 0, 0.0, false, m_line};
            }

            Token ReadNumber() {
                size_t start = m_pos;
                bool isFloat = false;

                if (m_src[m_pos] == '-') {
                    m_pos++;
                }

                while (m_pos < m_src.size() && (std::isdigit(static_cast<unsigned char>(m_src[m_pos])) || m_src[m_pos] == '.' || m_src[m_pos] == 'f' || m_src[m_pos] == 'F' || m_src[m_pos] == 'e' || m_src[m_pos] == 'E')) {
                    if (m_src[m_pos] == '.' || m_src[m_pos] == 'e' || m_src[m_pos] == 'E' || m_src[m_pos] == 'f' || m_src[m_pos] == 'F') {
                        isFloat = true;
                    }
                    m_pos++;
                }

                std::string numStr = m_src.substr(start, m_pos - start);
                // Strip trailing f/F if present
                if (!numStr.empty() && (numStr.back() == 'f' || numStr.back() == 'F')) {
                    numStr.pop_back();
                }

                if (isFloat) {
                    double fval = std::strtod(numStr.c_str(), nullptr);
                    return {TokenType::FloatLiteral, numStr, 0, fval, false, m_line};
                } else {
                    int64_t ival = std::strtoll(numStr.c_str(), nullptr, 10);
                    return {TokenType::IntegerLiteral, numStr, ival, static_cast<double>(ival), false, m_line};
                }
            }

            Token ReadIdentifier() {
                size_t start = m_pos;
                while (m_pos < m_src.size()) {
                    char c = m_src[m_pos];
                    if (std::isalnum(static_cast<unsigned char>(c)) || c == '_' || c == '/' || c == '\\' || c == '-' || c == '$') {
                        m_pos++;
                    } else {
                        break;
                    }
                }

                std::string ident = m_src.substr(start, m_pos - start);
                if (ident == "true") {
                    return {TokenType::BooleanLiteral, ident, 1, 1.0, true, m_line};
                } else if (ident == "false") {
                    return {TokenType::BooleanLiteral, ident, 0, 0.0, false, m_line};
                }

                return {TokenType::Identifier, ident, 0, 0.0, false, m_line};
            }

            const std::string& m_src;
            size_t m_pos;
            size_t m_line;
        };

    } // anonymous namespace

    bool GodParser::ParseFile(const std::string& filePath, GodScript& outScript) {
        std::ifstream file(filePath, std::ios::binary);
        if (!file.is_open()) {
            m_lastError = "Failed to open GOD script file: " + filePath;
            return false;
        }

        std::stringstream buffer;
        buffer << file.rdbuf();
        return ParseString(buffer.str(), outScript, filePath);
    }

    bool GodParser::ParseString(const std::string& scriptText, GodScript& outScript, const std::string& sourceName) {
        outScript.sourcePath = sourceName;
        Lexer lexer(scriptText);
        Token tok = lexer.NextToken();

        while (tok.type != TokenType::EndOfFile) {
            if (tok.type == TokenType::Identifier && tok.text == "BeginObject") {
                // Parse BeginObject <Type> <Name> or BeginObject("<Type>", "<Name>")
                Token typeTok = lexer.NextToken();
                if (typeTok.type == TokenType::OpenParen) {
                    typeTok = lexer.NextToken();
                }

                std::string typeName = typeTok.text;

                Token sepTok = lexer.NextToken();
                std::string objName;
                if (sepTok.type == TokenType::Comma) {
                    Token nameTok = lexer.NextToken();
                    objName = nameTok.text;
                    Token closeParen = lexer.NextToken();
                    if (closeParen.type != TokenType::CloseParen) {
                        // ignore or consume
                    }
                } else {
                    objName = sepTok.text;
                }

                auto currentObj = std::make_shared<GodObject>(typeName, objName);

                // Parse statements inside BeginObject until EndObject
                Token innerTok = lexer.NextToken();
                while (innerTok.type != TokenType::EndOfFile && !(innerTok.type == TokenType::Identifier && innerTok.text == "EndObject")) {
                    if (innerTok.type == TokenType::Identifier) {
                        std::string target = innerTok.text;
                        std::string methodName;

                        Token next = lexer.NextToken();
                        if (next.type == TokenType::Dot || next.type == TokenType::Colon) {
                            Token mTok = lexer.NextToken();
                            methodName = mTok.text;
                            next = lexer.NextToken();
                        } else {
                            methodName = target;
                        }

                        if (next.type == TokenType::OpenParen) {
                            // Parse arguments
                            std::vector<GodValue> args;
                            Token argTok = lexer.NextToken();
                            while (argTok.type != TokenType::CloseParen && argTok.type != TokenType::EndOfFile) {
                                if (argTok.type == TokenType::StringLiteral) {
                                    args.push_back(argTok.text);
                                } else if (argTok.type == TokenType::IntegerLiteral) {
                                    args.push_back(argTok.intValue);
                                } else if (argTok.type == TokenType::FloatLiteral) {
                                    args.push_back(argTok.floatValue);
                                } else if (argTok.type == TokenType::BooleanLiteral) {
                                    args.push_back(argTok.boolValue);
                                } else if (argTok.type == TokenType::Identifier) {
                                    // Could be variable or named constant or string
                                    args.push_back(argTok.text);
                                }

                                Token commaOrClose = lexer.NextToken();
                                if (commaOrClose.type == TokenType::Comma) {
                                    argTok = lexer.NextToken();
                                } else if (commaOrClose.type == TokenType::CloseParen) {
                                    break;
                                } else {
                                    argTok = commaOrClose;
                                }
                            }

                            currentObj->AddMethodCall(methodName, args);
                        } else if (next.type == TokenType::Assign) {
                            Token valTok = lexer.NextToken();
                            if (valTok.type == TokenType::StringLiteral) {
                                currentObj->SetProperty(methodName, valTok.text);
                            } else if (valTok.type == TokenType::IntegerLiteral) {
                                currentObj->SetProperty(methodName, valTok.intValue);
                            } else if (valTok.type == TokenType::FloatLiteral) {
                                currentObj->SetProperty(methodName, valTok.floatValue);
                            } else if (valTok.type == TokenType::BooleanLiteral) {
                                currentObj->SetProperty(methodName, valTok.boolValue);
                            } else {
                                currentObj->SetProperty(methodName, valTok.text);
                            }
                        }
                    }

                    innerTok = lexer.NextToken();
                }

                // If closed with EndObject(), consume optional ()
                Token afterEnd = lexer.NextToken();
                if (afterEnd.type == TokenType::OpenParen) {
                    Token closeP = lexer.NextToken();
                    tok = lexer.NextToken();
                } else {
                    tok = afterEnd;
                }

                outScript.objects.push_back(currentObj);
                outScript.objectsByName[currentObj->GetObjectName()] = currentObj;
            } else {
                tok = lexer.NextToken();
            }
        }

        return true;
    }

} // namespace Asset
} // namespace CTTR
