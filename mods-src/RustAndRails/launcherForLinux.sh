#!/bin/bash
VINTAGE_PATH="${AppData}/vintagestory/"
MOD_PATH="${PWD}/../../mods/rustandrails"
ASSET_PATH="${PWD}/../../mods"
cd ${VINTAGE_PATH}
mono Vintagestory.exe -oTestworld -pcreativebuilding --addModPath "${MOD_PATH}" --addOrigin "${ASSET_PATH}"
