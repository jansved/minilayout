cmake_minimum_required(VERSION 3.25)
project(native_engine CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Enable compiler warnings and visibility settings
if(MSVC)
    add_compile_options(/W4 /permissive-)
else()
    add_compile_options(-Wall -Wextra -Wpedantic)
endif()

# Find dependencies from vcpkg
find_package(glm CONFIG REQUIRED)
find_package(spdlog CONFIG REQUIRED)

add_library(native_engine SHARED
    src/engine.cpp
)

target_include_directories(native_engine
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

target_compile_definitions(native_engine
    PRIVATE
        ENGINE_EXPORTS
)

target_link_libraries(native_engine
    PRIVATE
        glm::glm
        spdlog::spdlog
)

set_target_properties(native_engine PROPERTIES
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON
    OUTPUT_NAME "native_engine"
)
