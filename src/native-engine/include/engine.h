#pragma once

#include <stdint.h>

#if defined(_WIN32)
    #if defined(ENGINE_EXPORTS)
        #define ENGINE_API __declspec(dllexport)
    #else
        #define ENGINE_API __declspec(dllimport)
    #endif
#else
    #if defined(ENGINE_EXPORTS)
        #define ENGINE_API __attribute__((visibility("default")))
    #else
        #define ENGINE_API
    #endif
#endif

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Native 3D bounding box representation matching .NET layout.
 */
typedef struct {
    int32_t id;
    float x;
    float y;
    float z;
    float width;
    float height;
    float depth;
} NativeBox;

/**
 * Checks collision between two 3D AABBs.
 * Returns 1 if intersecting (colliding), 0 otherwise.
 */
ENGINE_API int32_t engine_check_pair_collision(const NativeBox* a, const NativeBox* b);

/**
 * Evaluates all components in the scene against each other for 3D AABB collisions.
 * out_collision_flags is an array of size count, where each element is set to 1 if
 * that component collides with at least one other component, or 0 if it is clear.
 * Returns the total count of colliding components.
 */
ENGINE_API int32_t engine_evaluate_scene(const NativeBox* boxes, int32_t count, int32_t* out_collision_flags);

#ifdef __cplusplus
}
#endif
