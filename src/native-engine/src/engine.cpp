#include "../include/engine.h"

#include <glm/glm.hpp>
#include <spdlog/spdlog.h>

#include <cmath>
#include <cstring>

namespace {

// Checks whether two 1D intervals [c1 - e1/2, c1 + e1/2] and [c2 - e2/2, c2 + e2/2] overlap.
inline bool overlaps_1d(float c1, float e1, float c2, float e2) noexcept {
    // Overlapping condition: distance between centers <= sum of half extents
    return std::abs(c1 - c2) <= (0.5f * (e1 + e2));
}

} // namespace

extern "C" {

int32_t engine_check_pair_collision(const NativeBox* a, const NativeBox* b) {
    if (!a || !b) {
        return 0;
    }

    // Two AABBs collide if and only if they overlap on all three axes (X, Y, Z)
    const bool overlap_x = overlaps_1d(a->x, a->width, b->x, b->width);
    const bool overlap_y = overlaps_1d(a->y, a->height, b->y, b->height);
    const bool overlap_z = overlaps_1d(a->z, a->depth, b->z, b->depth);

    return (overlap_x && overlap_y && overlap_z) ? 1 : 0;
}

int32_t engine_evaluate_scene(const NativeBox* boxes, int32_t count, int32_t* out_collision_flags) {
    if (!boxes || count <= 0 || !out_collision_flags) {
        return 0;
    }

    // Initialize all flags to clear (0)
    std::memset(out_collision_flags, 0, sizeof(int32_t) * static_cast<size_t>(count));

    int32_t total_colliding_components = 0;

    // Evaluate all unique pairs (i, j) where i < j
    for (int32_t i = 0; i < count; ++i) {
        for (int32_t j = i + 1; j < count; ++j) {
            if (engine_check_pair_collision(&boxes[i], &boxes[j])) {
                out_collision_flags[i] = 1;
                out_collision_flags[j] = 1;
            }
        }
    }

    // Count components marked in collision
    for (int32_t i = 0; i < count; ++i) {
        if (out_collision_flags[i] != 0) {
            ++total_colliding_components;
        }
    }

    spdlog::info("[NativeEngine] Evaluated scene with {} components: {} in collision",
                 count, total_colliding_components);

    return total_colliding_components;
}

} // extern "C"
