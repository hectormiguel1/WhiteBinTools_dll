#ifndef WBT_SHARED_H
#define WBT_SHARED_H

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
    typedef unsigned char WBT_BOOL;

    // Game Identifier Enum
    typedef enum {
        FF131 = 0,
        FF132 = 1
    } GameCode;
    
    /* =======================================================================
 * Struct Definitions (Must match NativeStructs.cs layout)
 * ======================================================================= */
    
    typedef struct {
        int chunk_index;
        unsigned long file_code;
        unsigned int file_type_id; // Used for ff13-2
        char* file_path;      // UTF-8 String
    } FileEntry;

    typedef struct {
        FileEntry* items;     // Array pointer
        unsigned int count;
    } FileEntryList;

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