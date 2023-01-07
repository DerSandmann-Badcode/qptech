#!/bin/bash
VINTAGE_PATH="${HOME}/Downloads/vs_archive_1.17.9/vintagestory/"
MOD_PATH="${PWD}/../../mods/rustandrails"
ASSET_PATH="${PWD}/../../mods/rustandrails/assets"
cd ${VINTAGE_PATH}
mono Vintagestory.exe -oTestworld -pcreativebuilding --addModPath "${MOD_PATH}" --addOrigin "${ASSET_PATH}"
