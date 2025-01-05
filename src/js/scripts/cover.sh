node ./node_modules/vitest/vitest.mjs run \
  --coverage.enabled --coverage.thresholds.100 --coverage.include=**/sideload/*.mjs \
  --coverage.exclude=**/dotnet.* --coverage.allowExternal
