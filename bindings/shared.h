#ifndef WBT_SHARED_H
#define WBT_SHARED_H

#include <stddef.h>
#include <stdint.h>

/* =======================================================================
 * Platform & Visibility Macros
 * ======================================================================= */
#if defined(_WIN32)
    #ifdef WBT_EXPORTS
        #define WBT_API __declspec(dllexport)
    #else
        #define WBT_API __declspec(dllimport)
    #endif
#else
    #define WBT_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

    /* =======================================================================
     * Common Types
     * ======================================================================= */

    // Represents a boolean value (0 = false, 1 = true).
    // Using uint8_t ensures 1-byte alignment to match C# 'byte'.
    typedef uint8_t WbtBool;

    // Game Identifier Enum
    typedef enum {
        WBT_GAME_FF131 = 0,
        WBT_GAME_FF132 = 1
    } GameCode;

    /* =======================================================================
     * Error Codes
     * ======================================================================= */
#define WBT_SUCCESS 0
#define WBT_ERROR_GENERAL 1
#define WBT_ERROR_INVALID_ARGS -1
#define WBT_ERROR_FILE_NOT_FOUND 2

#ifdef __cplusplus
}
#endif

#endif // WBT_SHARED_H