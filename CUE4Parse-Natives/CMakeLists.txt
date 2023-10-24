cmake_minimum_required(VERSION 3.5)
project(CUE4Parse-Natives CXX)

set(CMAKE_CXX_STANDARD 11)


if (EXISTS "${PROJECT_SOURCE_DIR}/ACL/external/acl/includes")
    set(WITH_ACL 1)
endif()

if (EXISTS ${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core)
    set(WITH_Oodle 1)
endif()


# ACL
if (WITH_ACL)
        add_compile_definitions(WITH_ACL)
        include_directories("${PROJECT_SOURCE_DIR}/ACL/external/acl/includes")
        include_directories("${PROJECT_SOURCE_DIR}/ACL/external/acl/external/rtm/includes")
        set(SOURCES ${SOURCES} ${PROJECT_SOURCE_DIR}/ACL/ACL.cpp)
endif()

# Oodle
if (WITH_Oodle)
        include_directories(${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core)
        include_directories(${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/base)
        include_directories(${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core/public)
        file(GLOB OODLE_SOURCES ${PROJECT_SOURCE_DIR}/Oodle/external/oodle2/core/*.cpp)

        set(SOURCES ${SOURCES} ${PROJECT_SOURCE_DIR}/Oodle/Oodle.cpp ${OODLE_SOURCES})

        add_compile_definitions(OODLE_BUILDING_LIB)
        add_compile_definitions(WITH_Oodle)
endif()


set(SOURCES ${SOURCES} ${PROJECT_SOURCE_DIR}/Features.cpp)
include_directories("${PROJECT_SOURCE_DIR}/common")


add_library("${PROJECT_NAME}" SHARED ${SOURCES})
set_target_properties("${PROJECT_NAME}" PROPERTIES PREFIX "")

if (WIN32)
    add_compile_definitions(WIN_EXPORT)
endif()

if (MSVC)
    install(FILES $<TARGET_PDB_FILE:${PROJECT_NAME}>
            DESTINATION "${PROJECT_SOURCE_DIR}/bin/Debug/" OPTIONAL) 
endif()

install(TARGETS CUE4Parse-Natives
        CONFIGURATIONS Debug
        RUNTIME DESTINATION "${PROJECT_SOURCE_DIR}/bin/Debug/")
install(TARGETS CUE4Parse-Natives
        CONFIGURATIONS Release
        RUNTIME DESTINATION "${PROJECT_SOURCE_DIR}/bin/Release/")