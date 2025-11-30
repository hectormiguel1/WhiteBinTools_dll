#ifndef WBT_UNPACK_H
#define WBT_UNPACK_H

#include "shared.h"

#ifdef __cplusplus
extern "C" {
#endif
    
    /* =======================================================================
     * Unpack Functions
     * ======================================================================= */

    /**
     * @brief Parses the filelist and returns metadata for all files.
     * @warning You MUST call free_metadata() on the result to avoid memory leaks.
     */
    WBT_API FileEntryList get_file_metadata(
        GameCode gameCodeRaw, 
        const char* filelist_path
    );

    /**
     * @brief Frees the memory allocated by get_file_metadata.
     */
    WBT_API void free_metadata(FileEntryList list);

    /**
     * @brief Extracts all files found in the filelist.
     */
    WBT_API WBT_STATUS unpack_all(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path
    );
    
    /**
 * @brief Extracts all files found in the filelist. to specified outDir
 */
    WBT_API WBT_STATUS unpack_all_to_path(
        GameCode gameCodeRaw, 
        const char* filelist_path,
        const char* white_bin_path,
        const char* outDir
        );

    /**
     * @brief Extracts a single file based on exact internal path matching.
     */
    WBT_API WBT_STATUS unpack_single(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path, 
        const char* target_path
    );
    
    /**
 * @brief Extracts a single file based on exact internal path matching to specified outDir
 */
    WBT_API WBT_STATUS unpack_single_to_path(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path, 
        const char* target_path,
        const char* outDir
    );

    /**
     * @brief Extracts multiple files matching a directory pattern.
     */
    WBT_API WBT_STATUS unpack_multiple(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path, 
        const char* directory_filter
    );
    
    /**
 * @brief Extracts multiple files matching a directory pattern. to specified outDir
 */
    WBT_API WBT_STATUS unpack_multiple_to_path(
        GameCode gameCodeRaw, 
        const char* filelist_path, 
        const char* white_bin_path, 
        const char* directory_filter, 
        const char* outDir
    );

#ifdef __cplusplus
}
#endif

#endif // WBT_UNPACK_H