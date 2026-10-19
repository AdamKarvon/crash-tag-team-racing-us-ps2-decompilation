#pragma once
#include "../parser/P3DParser.h"
#include "../parser/ParsedP3D.h"

namespace CTTR {
namespace Asset {

    using P3DParser = CTTR::Parser::P3DParser;
    using ParsedP3DFile = CTTR::Parser::ParsedP3DFile;
    using ParsedMesh = CTTR::Parser::ParsedMesh;
    using ParsedVertex = CTTR::Parser::ParsedVertex;
    using ParsedTexture = CTTR::Parser::ParsedTexture;
    using ParsedShader = CTTR::Parser::ParsedShader;
    using ParsedBone = CTTR::Parser::ParsedBone;
    using ParsedSkeleton = CTTR::Parser::ParsedSkeleton;
    using ParsedLocator = CTTR::Parser::ParsedLocator;
    using ParsedCollisionObject = CTTR::Parser::ParsedCollisionObject;
    using ParsedCollisionSphere = CTTR::Parser::ParsedCollisionSphere;
    using ParsedCollisionOBB = CTTR::Parser::ParsedCollisionOBB;
    using ParsedCollisionAABB = CTTR::Parser::ParsedCollisionAABB;
    using ParsedCollisionTriangle = CTTR::Parser::ParsedCollisionTriangle;

} // namespace Asset
} // namespace CTTR
