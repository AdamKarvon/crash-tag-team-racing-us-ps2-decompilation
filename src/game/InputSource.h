#pragma once
// =============================================================================
//  InputSource.h  —  Driver input abstraction
//  A kart is driven by an InputSource each fixed tick. Keyboard today;
//  AI (Phase 2) and network (Phase 5) sources plug into the same interface.
// =============================================================================

namespace CTTR {
namespace Game {

    struct DriverInput {
        float throttle   = 0.0f;  // 0..1
        float steering   = 0.0f;  // -1 (left) .. +1 (right)
        bool  brake      = false;
        bool  powerslide = false;
        bool  turbo      = false;
    };

    class InputSource {
    public:
        virtual ~InputSource() = default;
        // Called once per fixed physics tick.
        virtual DriverInput Poll() = 0;
    };

    // Reads a GLFW-style key state table (indexed by GLFW key code).
    // Key codes are passed in so this header stays free of GLFW includes.
    class KeyboardInput : public InputSource {
    public:
        struct Bindings {
            int throttle[2];
            int brake[2];
            int left[2];
            int right[2];
            int powerslide;
            int turbo;
        };

        KeyboardInput(const bool* keyTable, int keyTableSize, const Bindings& bindings)
            : m_keys(keyTable), m_size(keyTableSize), m_bind(bindings) {}

        // While > 0, forces full throttle (used by --drive-test).
        void SetForcedThrottleTicks(int ticks) { m_forcedThrottleTicks = ticks; }
        bool IsForcingThrottle() const { return m_forcedThrottleTicks > 0; }

        // When disabled (e.g. free camera active) the kart receives neutral input.
        void SetEnabled(bool enabled) { m_enabled = enabled; }

        DriverInput Poll() override {
            DriverInput in;
            if (m_enabled) {
                if (Down(m_bind.throttle[0]) || Down(m_bind.throttle[1])) in.throttle += 1.0f;
                if (Down(m_bind.brake[0])    || Down(m_bind.brake[1]))    in.brake = true;
                if (Down(m_bind.left[0])     || Down(m_bind.left[1]))     in.steering -= 1.0f;
                if (Down(m_bind.right[0])    || Down(m_bind.right[1]))    in.steering += 1.0f;
                in.powerslide = Down(m_bind.powerslide);
                in.turbo      = Down(m_bind.turbo);
            }
            if (m_forcedThrottleTicks > 0) {
                in.throttle = 1.0f;
                --m_forcedThrottleTicks;
            }
            return in;
        }

    private:
        bool Down(int key) const { return key >= 0 && key < m_size && m_keys[key]; }

        const bool* m_keys;
        int m_size;
        Bindings m_bind;
        int m_forcedThrottleTicks = 0;
        bool m_enabled = true;
    };

} // namespace Game
} // namespace CTTR
