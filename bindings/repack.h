#ifndef WBT_REPACK_H
#define WBT_REPACK_H

#include "shared.h"

#ifdef __cplusplus
extern "C" {
#endif

    /**
     * @brief Repacks all files from a directory into a new WhiteBin.
     * Corresponds to RepackTypeA.
     */
    WBT_API WBT_STATUS repack_all(
        GameCode game_code, 
        const char* filelist_path, 
        const char* src_dir, 
        WBT_BOOL make_backup
    );

    /**
     * @brief Repacks a single file into an existing WhiteBin.
     * Corresponds to RepackTypeB.
     */
    WBT_API WBT_STATUS repack_single(
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
    WBT_API WBT_STATUS repack_multiple(
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
    WBT_API WBT_STATUS repack_filelist_from_chunks(
        GameCode game_code, 
        const char* chunk_dir, 
        WBT_BOOL make_backup
    );

    /**
     * @brief Repacks a filelist from a JSON source file.
     * Corresponds to RepackTypeE.
     */
    WBT_API WBT_STATUS repack_filelist_from_json(
        GameCode game_code, 
        const char* json_path, 
        WBT_BOOL make_backup
    );

#ifdef __cplusplus
}
#endif

#endif // WBT_REPACK_H