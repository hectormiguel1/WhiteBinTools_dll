#ifndef WBT_LIB_H
#define WBT_LIB_H

#include "common.h"

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
     * Shared Domain Types
     * ======================================================================= */

    // Represents a boolean value (0 = false, 1 = true).
    // Using uint8_t ensures 1-byte alignment to match C# 'byte'.
    typedef unsigned char WBT_BOOL;

    // Game Identifier Enum
    typedef enum {
        FF131 = 0,
        FF132 = 1
    } GameCode;
    
    // Struct Definitions (Must match NativeStructs.cs layout)
    
    typedef struct {
        int chunk_index;
        unsigned long file_code;
        unsigned int file_type_id; // Used for ff13-2
        char* file_path;           // UTF-8 String
    } FileEntry;

    typedef struct {
        FileEntry* items;          // Array pointer
        int count;                 
    } FileEntryList;

    /* =======================================================================
     * Repack API
     * ======================================================================= */

    /**
     * @brief Repacks all files from a directory into a new WhiteBin.
     * Corresponds to RepackTypeA.
     */
    WBT_API Result repack_all(
        GameCode game_code, 
        const char* filelist_path, 
        const char* src_dir, 
        WBT_BOOL make_backup
    );

    /**
     * @brief Repacks a single file into an existing WhiteBin.
     * Corresponds to RepackTypeB.
     */
    WBT_API Result repack_single(
        GameCode game_code, 
        const char* filelist_path, 
        const char* bin_path, 
        const char* target_file, 
        WBT_BOOL make_backup
    );

    /**
     * @brief Repacks multiple detected files into an existing WhiteBin.
     * Corresponds to RepackTypeC.
     */
    WBT_API Result repack_multiple(
        GameCode game_code, 
        const char* filelist_path, 
        const char* bin_path, 
        const char* extract_dir, 
        WBT_BOOL make_backup
    );

    /**
     * @brief Repacks a filelist from raw Text Chunk files.
     * Corresponds to RepackTypeD.
     */
    WBT_API Result repack_filelist_from_chunks(
        GameCode game_code, 
        const char* chunk_dir, 
        WBT_BOOL make_backup
    );

    /**
     * @brief Repacks a filelist from a JSON source file.
     * Corresponds to RepackTypeE.
     */
    WBT_API Result repack_filelist_from_json(
        GameCode game_code, 
        const char* json_path, 
        WBT_BOOL make_backup
    );

    /* =======================================================================
     * Unpack API
     * ======================================================================= */

    /**
     * @brief Parses the filelist and returns metadata for all files.
     * @warning You MUST call free_result() on the result to avoid memory leaks.
     */
    WBT_API Result get_file_metadata(
        GameCode gameCodeRaw, 
        const char* filelist_path
    );

    /**
     * @brief Extracts all files found in the filelist.
     */
    WBT_API Result unpack_all(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path
    );
    
    /**
     * @brief Extracts all files found in the filelist to specified outDir.
     */
    WBT_API Result unpack_all_to_path(
        GameCode gameCodeRaw, 
        const char* filelist_path,
        const char* white_bin_path,
        const char* outDir
    );

    /**
     * @brief Extracts a single file based on exact internal path matching.
     */
    WBT_API Result unpack_single(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path, 
        const char* target_path
    );
    
    /**
     * @brief Extracts a single file based on exact internal path matching to specified outDir.
     */
    WBT_API Result unpack_single_to_path(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path, 
        const char* target_path,
        const char* outDir
    );

    /**
     * @brief Extracts multiple files matching a directory pattern.
     */
    WBT_API Result unpack_multiple(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path, 
        const char* directory_filter
    );
    
    /**
     * @brief Extracts multiple files matching a directory pattern to specified outDir.
     */
    WBT_API Result unpack_multiple_to_path(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path, 
        const char* directory_filter, 
        const char* outDir
    );

#ifdef __cplusplus
}
#endif

#endif // WBT_LIB_H