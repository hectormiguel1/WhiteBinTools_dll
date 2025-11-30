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
    // --- Logging ---
    // Callback receives a pointer to a UTF-8 string allocated on the heap.
    typedef void (*LogCallback)(const char* message);

    // Register logging callback. Pass NULL to reset to default console output.
    WBT_API void set_logging_callback(LogCallback callback);

    // [NEW] Free the memory allocated for the log message.
    // Must be called by the consumer (Dart/C) after processing the log string.
    WBT_API void free_log_memory(void* ptr);
    // Game Identifier Enum
    typedef enum {
        FF131 = 0,
        FF132 = 1
    } GameCode;
    
    typedef enum
    {
        FILE_NOT_FOUND = -2,
        INVLID_ARGS = -1,
        SUCCESS = 0, 
        EXCEPTION_ERROR = 1,
        
    } WBT_STATUS;
    
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
    
    //Register a call back to be invoked when logging messages.
    void set_logging_callback(void (*callback)(const char*));

#ifdef __cplusplus
}
#endif

#endif // WBT_SHARED_H