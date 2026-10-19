# Third-party notices

## NetP3DLib

The UI exporter depends on the bundled NetP3DLib source under
`NetP3DLib-main/`. It is distributed under the MIT License; see
`NetP3DLib-main/LICENSE` for its copyright and license terms.

## GLAD and Khronos headers

`external/glad/` contains GLAD 0.1.36 generated OpenGL loader code and the
Khronos `khrplatform.h` header. Their upstream notices are retained in the
source files.

## CMake-fetched dependencies

The CMake build fetches GLFW and nlohmann/json into the local build directory.
Their upstream license files are provided with those dependencies when fetched;
their source is not vendored in this repository.
