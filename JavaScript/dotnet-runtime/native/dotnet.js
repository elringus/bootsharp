var Module = (function () {
    var _scriptDir = typeof document !== 'undefined' && document.currentScript ? document.currentScript.src : undefined;

    return (
        function (Module) {
            Module = Module || {};

            var Module = typeof Module !== "undefined" ? Module : {};
            var readyPromiseResolve,
                readyPromiseReject;
            Module["ready"] = new Promise(function (resolve, reject) {
                readyPromiseResolve = resolve;
                readyPromiseReject = reject
            });
            var moduleOverrides = {};
            var key;
            for (key in Module) {if (Module.hasOwnProperty(key)) {moduleOverrides[key] = Module[key]}}
            var arguments_ = [];
            var thisProgram = "./this.program";
            var quit_ = function (status, toThrow) {throw toThrow};
            var ENVIRONMENT_IS_WEB = true;
            var ENVIRONMENT_IS_WORKER = false;
            var ENVIRONMENT_IS_NODE = false;
            var ENVIRONMENT_IS_SHELL = false;
            var scriptDirectory = "";

            function locateFile(path) {
                if (Module["locateFile"]) {return Module["locateFile"](path, scriptDirectory)}
                return scriptDirectory + path
            }

            var read_,
                readAsync,
                readBinary,
                setWindowTitle;
            if (ENVIRONMENT_IS_WEB || ENVIRONMENT_IS_WORKER) {
                if (ENVIRONMENT_IS_WORKER) {scriptDirectory = self.location.href} else if (typeof document !== "undefined" && document.currentScript) {scriptDirectory = document.currentScript.src}
                if (_scriptDir) {scriptDirectory = _scriptDir}
                if (scriptDirectory.indexOf("blob:") !== 0) {scriptDirectory = scriptDirectory.substr(0, scriptDirectory.lastIndexOf("/") + 1)} else {scriptDirectory = ""}
                {
                    read_ = function (url) {
                        var xhr = new XMLHttpRequest;
                        xhr.open("GET", url, false);
                        xhr.send(null);
                        return xhr.responseText
                    };
                    if (ENVIRONMENT_IS_WORKER) {
                        readBinary = function (url) {
                            var xhr = new XMLHttpRequest;
                            xhr.open("GET", url, false);
                            xhr.responseType = "arraybuffer";
                            xhr.send(null);
                            return new Uint8Array(xhr.response)
                        }
                    }
                    readAsync = function (url, onload, onerror) {
                        var xhr = new XMLHttpRequest;
                        xhr.open("GET", url, true);
                        xhr.responseType = "arraybuffer";
                        xhr.onload = function () {
                            if (xhr.status == 200 || xhr.status == 0 && xhr.response) {
                                onload(xhr.response);
                                return
                            }
                            onerror()
                        };
                        xhr.onerror = onerror;
                        xhr.send(null)
                    }
                }
                setWindowTitle = function (title) {document.title = title}
            } else {}
            var out = Module["print"] || console.log.bind(console);
            var err = Module["printErr"] || console.warn.bind(console);
            for (key in moduleOverrides) {if (moduleOverrides.hasOwnProperty(key)) {Module[key] = moduleOverrides[key]}}
            moduleOverrides = null;
            if (Module["arguments"]) arguments_ = Module["arguments"];
            if (Module["thisProgram"]) thisProgram = Module["thisProgram"];
            if (Module["quit"]) quit_ = Module["quit"];
            var STACK_ALIGN = 16;

            function alignMemory(size, factor) {
                if (!factor) factor = STACK_ALIGN;
                return Math.ceil(size / factor) * factor
            }

            function warnOnce(text) {
                if (!warnOnce.shown) warnOnce.shown = {};
                if (!warnOnce.shown[text]) {
                    warnOnce.shown[text] = 1;
                    err(text)
                }
            }

            function convertJsFunctionToWasm(func, sig) {
                if (typeof WebAssembly.Function === "function") {
                    var typeNames = { "i": "i32", "j": "i64", "f": "f32", "d": "f64" };
                    var type = { parameters: [], results: sig[0] == "v" ? [] : [typeNames[sig[0]]] };
                    for (var i = 1; i < sig.length; ++i) {type.parameters.push(typeNames[sig[i]])}
                    return new WebAssembly.Function(type, func)
                }
                var typeSection = [1, 0, 1, 96];
                var sigRet = sig.slice(0, 1);
                var sigParam = sig.slice(1);
                var typeCodes = { "i": 127, "j": 126, "f": 125, "d": 124 };
                typeSection.push(sigParam.length);
                for (var i = 0; i < sigParam.length; ++i) {typeSection.push(typeCodes[sigParam[i]])}
                if (sigRet == "v") {typeSection.push(0)} else {typeSection = typeSection.concat([1, typeCodes[sigRet]])}
                typeSection[1] = typeSection.length - 2;
                var bytes = new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0].concat(typeSection, [2, 7, 1, 1, 101, 1, 102, 0, 0, 7, 5, 1, 1, 102, 0, 0]));
                var module = new WebAssembly.Module(bytes);
                var instance = new WebAssembly.Instance(module, { "e": { "f": func } });
                var wrappedFunc = instance.exports["f"];
                return wrappedFunc
            }

            var freeTableIndexes = [];
            var functionsInTableMap;

            function getEmptyTableSlot() {
                if (freeTableIndexes.length) {return freeTableIndexes.pop()}
                try {wasmTable.grow(1)} catch (err) {
                    if (!(err instanceof RangeError)) {throw err}
                    throw"Unable to grow wasm table. Set ALLOW_TABLE_GROWTH."
                }
                return wasmTable.length - 1
            }

            function addFunctionWasm(func, sig) {
                if (!functionsInTableMap) {
                    functionsInTableMap = new WeakMap;
                    for (var i = 0; i < wasmTable.length; i++) {
                        var item = wasmTable.get(i);
                        if (item) {functionsInTableMap.set(item, i)}
                    }
                }
                if (functionsInTableMap.has(func)) {return functionsInTableMap.get(func)}
                var ret = getEmptyTableSlot();
                try {wasmTable.set(ret, func)} catch (err) {
                    if (!(err instanceof TypeError)) {throw err}
                    var wrapped = convertJsFunctionToWasm(func, sig);
                    wasmTable.set(ret, wrapped)
                }
                functionsInTableMap.set(func, ret);
                return ret
            }

            function addFunction(func, sig) {return addFunctionWasm(func, sig)}

            var tempRet0 = 0;
            var setTempRet0 = function (value) {tempRet0 = value};
            var getTempRet0 = function () {return tempRet0};
            var wasmBinary;
            if (Module["wasmBinary"]) wasmBinary = Module["wasmBinary"];
            var noExitRuntime = Module["noExitRuntime"] || true;
            if (typeof WebAssembly !== "object") {abort("no native wasm support detected")}

            function setValue(ptr, value, type, noSafe) {
                type = type || "i8";
                if (type.charAt(type.length - 1) === "*") type = "i32";
                switch (type) {
                    case"i1":
                        HEAP8[ptr >> 0] = value;
                        break;
                    case"i8":
                        HEAP8[ptr >> 0] = value;
                        break;
                    case"i16":
                        HEAP16[ptr >> 1] = value;
                        break;
                    case"i32":
                        HEAP32[ptr >> 2] = value;
                        break;
                    case"i64":
                        tempI64 = [value >>> 0, (tempDouble = value, +Math.abs(tempDouble) >= 1 ? tempDouble > 0 ? (Math.min(+Math.floor(tempDouble / 4294967296), 4294967295) | 0) >>> 0 : ~~+Math.ceil((tempDouble - +(~~tempDouble >>> 0)) / 4294967296) >>> 0 : 0)], HEAP32[ptr >> 2] = tempI64[0], HEAP32[ptr + 4 >> 2] = tempI64[1];
                        break;
                    case"float":
                        HEAPF32[ptr >> 2] = value;
                        break;
                    case"double":
                        HEAPF64[ptr >> 3] = value;
                        break;
                    default:
                        abort("invalid type for setValue: " + type)
                }
            }

            function getValue(ptr, type, noSafe) {
                type = type || "i8";
                if (type.charAt(type.length - 1) === "*") type = "i32";
                switch (type) {
                    case"i1":
                        return HEAP8[ptr >> 0];
                    case"i8":
                        return HEAP8[ptr >> 0];
                    case"i16":
                        return HEAP16[ptr >> 1];
                    case"i32":
                        return HEAP32[ptr >> 2];
                    case"i64":
                        return HEAP32[ptr >> 2];
                    case"float":
                        return HEAPF32[ptr >> 2];
                    case"double":
                        return HEAPF64[ptr >> 3];
                    default:
                        abort("invalid type for getValue: " + type)
                }
                return null
            }

            var wasmMemory;
            var ABORT = false;
            var EXITSTATUS;

            function assert(condition, text) {if (!condition) {abort("Assertion failed: " + text)}}

            function getCFunc(ident) {
                var func = Module["_" + ident];
                assert(func, "Cannot call unknown function " + ident + ", make sure it is exported");
                return func
            }

            function ccall(ident, returnType, argTypes, args, opts) {
                var toC = {
                    "string": function (str) {
                        var ret = 0;
                        if (str !== null && str !== undefined && str !== 0) {
                            var len = (str.length << 2) + 1;
                            ret = stackAlloc(len);
                            stringToUTF8(str, ret, len)
                        }
                        return ret
                    }, "array": function (arr) {
                        var ret = stackAlloc(arr.length);
                        writeArrayToMemory(arr, ret);
                        return ret
                    }
                };

                function convertReturnValue(ret) {
                    if (returnType === "string") return UTF8ToString(ret);
                    if (returnType === "boolean") return Boolean(ret);
                    return ret
                }

                var func = getCFunc(ident);
                var cArgs = [];
                var stack = 0;
                if (args) {
                    for (var i = 0; i < args.length; i++) {
                        var converter = toC[argTypes[i]];
                        if (converter) {
                            if (stack === 0) stack = stackSave();
                            cArgs[i] = converter(args[i])
                        } else {cArgs[i] = args[i]}
                    }
                }
                var ret = func.apply(null, cArgs);
                ret = convertReturnValue(ret);
                if (stack !== 0) stackRestore(stack);
                return ret
            }

            function cwrap(ident, returnType, argTypes, opts) {
                argTypes = argTypes || [];
                var numericArgs = argTypes.every(function (type) {return type === "number"});
                var numericRet = returnType !== "string";
                if (numericRet && numericArgs && !opts) {return getCFunc(ident)}
                return function () {return ccall(ident, returnType, argTypes, arguments, opts)}
            }

            var ALLOC_STACK = 1;
            var UTF8Decoder = typeof TextDecoder !== "undefined" ? new TextDecoder("utf8") : undefined;

            function UTF8ArrayToString(heap, idx, maxBytesToRead) {
                var endIdx = idx + maxBytesToRead;
                var endPtr = idx;
                while (heap[endPtr] && !(endPtr >= endIdx)) ++endPtr;
                if (endPtr - idx > 16 && heap.subarray && UTF8Decoder) {return UTF8Decoder.decode(heap.subarray(idx, endPtr))} else {
                    var str = "";
                    while (idx < endPtr) {
                        var u0 = heap[idx++];
                        if (!(u0 & 128)) {
                            str += String.fromCharCode(u0);
                            continue
                        }
                        var u1 = heap[idx++] & 63;
                        if ((u0 & 224) == 192) {
                            str += String.fromCharCode((u0 & 31) << 6 | u1);
                            continue
                        }
                        var u2 = heap[idx++] & 63;
                        if ((u0 & 240) == 224) {u0 = (u0 & 15) << 12 | u1 << 6 | u2} else {u0 = (u0 & 7) << 18 | u1 << 12 | u2 << 6 | heap[idx++] & 63}
                        if (u0 < 65536) {str += String.fromCharCode(u0)} else {
                            var ch = u0 - 65536;
                            str += String.fromCharCode(55296 | ch >> 10, 56320 | ch & 1023)
                        }
                    }
                }
                return str
            }

            function UTF8ToString(ptr, maxBytesToRead) {return ptr ? UTF8ArrayToString(HEAPU8, ptr, maxBytesToRead) : ""}

            function stringToUTF8Array(str, heap, outIdx, maxBytesToWrite) {
                if (!(maxBytesToWrite > 0)) return 0;
                var startIdx = outIdx;
                var endIdx = outIdx + maxBytesToWrite - 1;
                for (var i = 0; i < str.length; ++i) {
                    var u = str.charCodeAt(i);
                    if (u >= 55296 && u <= 57343) {
                        var u1 = str.charCodeAt(++i);
                        u = 65536 + ((u & 1023) << 10) | u1 & 1023
                    }
                    if (u <= 127) {
                        if (outIdx >= endIdx) break;
                        heap[outIdx++] = u
                    } else if (u <= 2047) {
                        if (outIdx + 1 >= endIdx) break;
                        heap[outIdx++] = 192 | u >> 6;
                        heap[outIdx++] = 128 | u & 63
                    } else if (u <= 65535) {
                        if (outIdx + 2 >= endIdx) break;
                        heap[outIdx++] = 224 | u >> 12;
                        heap[outIdx++] = 128 | u >> 6 & 63;
                        heap[outIdx++] = 128 | u & 63
                    } else {
                        if (outIdx + 3 >= endIdx) break;
                        heap[outIdx++] = 240 | u >> 18;
                        heap[outIdx++] = 128 | u >> 12 & 63;
                        heap[outIdx++] = 128 | u >> 6 & 63;
                        heap[outIdx++] = 128 | u & 63
                    }
                }
                heap[outIdx] = 0;
                return outIdx - startIdx
            }

            function stringToUTF8(str, outPtr, maxBytesToWrite) {return stringToUTF8Array(str, HEAPU8, outPtr, maxBytesToWrite)}

            function lengthBytesUTF8(str) {
                var len = 0;
                for (var i = 0; i < str.length; ++i) {
                    var u = str.charCodeAt(i);
                    if (u >= 55296 && u <= 57343) u = 65536 + ((u & 1023) << 10) | str.charCodeAt(++i) & 1023;
                    if (u <= 127) ++len; else if (u <= 2047) len += 2; else if (u <= 65535) len += 3; else len += 4
                }
                return len
            }

            var UTF16Decoder = typeof TextDecoder !== "undefined" ? new TextDecoder("utf-16le") : undefined;

            function stringToUTF16(str, outPtr, maxBytesToWrite) {
                if (maxBytesToWrite === undefined) {maxBytesToWrite = 2147483647}
                if (maxBytesToWrite < 2) return 0;
                maxBytesToWrite -= 2;
                var startPtr = outPtr;
                var numCharsToWrite = maxBytesToWrite < str.length * 2 ? maxBytesToWrite / 2 : str.length;
                for (var i = 0; i < numCharsToWrite; ++i) {
                    var codeUnit = str.charCodeAt(i);
                    HEAP16[outPtr >> 1] = codeUnit;
                    outPtr += 2
                }
                HEAP16[outPtr >> 1] = 0;
                return outPtr - startPtr
            }

            function allocateUTF8(str) {
                var size = lengthBytesUTF8(str) + 1;
                var ret = _malloc(size);
                if (ret) stringToUTF8Array(str, HEAP8, ret, size);
                return ret
            }

            function writeArrayToMemory(array, buffer) {HEAP8.set(array, buffer)}

            function writeAsciiToMemory(str, buffer, dontAddNull) {
                for (var i = 0; i < str.length; ++i) {HEAP8[buffer++ >> 0] = str.charCodeAt(i)}
                if (!dontAddNull) HEAP8[buffer >> 0] = 0
            }

            function alignUp(x, multiple) {
                if (x % multiple > 0) {x += multiple - x % multiple}
                return x
            }

            var buffer,
                HEAP8,
                HEAPU8,
                HEAP16,
                HEAPU16,
                HEAP32,
                HEAPU32,
                HEAPF32,
                HEAPF64;

            function updateGlobalBufferAndViews(buf) {
                buffer = buf;
                Module["HEAP8"] = HEAP8 = new Int8Array(buf);
                Module["HEAP16"] = HEAP16 = new Int16Array(buf);
                Module["HEAP32"] = HEAP32 = new Int32Array(buf);
                Module["HEAPU8"] = HEAPU8 = new Uint8Array(buf);
                Module["HEAPU16"] = HEAPU16 = new Uint16Array(buf);
                Module["HEAPU32"] = HEAPU32 = new Uint32Array(buf);
                Module["HEAPF32"] = HEAPF32 = new Float32Array(buf);
                Module["HEAPF64"] = HEAPF64 = new Float64Array(buf)
            }

            var INITIAL_MEMORY = Module["INITIAL_MEMORY"] || 16777216;
            var wasmTable;
            var __ATPRERUN__ = [];
            var __ATINIT__ = [];
            var __ATMAIN__ = [];
            var __ATPOSTRUN__ = [];
            var runtimeInitialized = false;
            var runtimeExited = false;

            function preRun() {
                if (Module["preRun"]) {
                    if (typeof Module["preRun"] == "function") Module["preRun"] = [Module["preRun"]];
                    while (Module["preRun"].length) {addOnPreRun(Module["preRun"].shift())}
                }
                callRuntimeCallbacks(__ATPRERUN__)
            }

            function initRuntime() {
                runtimeInitialized = true;
                if (!Module["noFSInit"] && !FS.init.initialized) FS.init();
                TTY.init();
                SOCKFS.root = FS.mount(SOCKFS, {}, null);
                callRuntimeCallbacks(__ATINIT__)
            }

            function exitRuntime() {runtimeExited = true}

            function postRun() {
                if (Module["postRun"]) {
                    if (typeof Module["postRun"] == "function") Module["postRun"] = [Module["postRun"]];
                    while (Module["postRun"].length) {addOnPostRun(Module["postRun"].shift())}
                }
                callRuntimeCallbacks(__ATPOSTRUN__)
            }

            function addOnPreRun(cb) {__ATPRERUN__.unshift(cb)}

            function addOnInit(cb) {__ATINIT__.unshift(cb)}

            function addOnPostRun(cb) {__ATPOSTRUN__.unshift(cb)}

            var runDependencies = 0;
            var runDependencyWatcher = null;
            var dependenciesFulfilled = null;

            function getUniqueRunDependency(id) {return id}

            function addRunDependency(id) {
                runDependencies++;
                if (Module["monitorRunDependencies"]) {Module["monitorRunDependencies"](runDependencies)}
            }

            function removeRunDependency(id) {
                runDependencies--;
                if (Module["monitorRunDependencies"]) {Module["monitorRunDependencies"](runDependencies)}
                if (runDependencies == 0) {
                    if (runDependencyWatcher !== null) {
                        clearInterval(runDependencyWatcher);
                        runDependencyWatcher = null
                    }
                    if (dependenciesFulfilled) {
                        var callback = dependenciesFulfilled;
                        dependenciesFulfilled = null;
                        callback()
                    }
                }
            }

            Module["preloadedImages"] = {};
            Module["preloadedAudios"] = {};

            function abort(what) {
                if (Module["onAbort"]) {Module["onAbort"](what)}
                what += "";
                err(what);
                ABORT = true;
                EXITSTATUS = 1;
                what = "abort(" + what + "). Build with -s ASSERTIONS=1 for more info.";
                var e = new WebAssembly.RuntimeError(what);
                readyPromiseReject(e);
                throw e
            }

            var dataURIPrefix = "data:application/octet-stream;base64,";

            function isDataURI(filename) {return filename.startsWith(dataURIPrefix)}

            var wasmBinaryFile = "dotnet.wasm";
            if (!isDataURI(wasmBinaryFile)) {wasmBinaryFile = locateFile(wasmBinaryFile)}

            function getBinary(file) {
                try {
                    if (file == wasmBinaryFile && wasmBinary) {return new Uint8Array(wasmBinary)}
                    if (readBinary) {return readBinary(file)} else {throw"both async and sync fetching of the wasm failed"}
                } catch (err) {abort(err)}
            }

            function getBinaryPromise() {
                if (!wasmBinary && (ENVIRONMENT_IS_WEB || ENVIRONMENT_IS_WORKER)) {
                    if (typeof fetch === "function") {
                        return fetch(wasmBinaryFile, { credentials: "same-origin" }).then(function (response) {
                            if (!response["ok"]) {throw"failed to load wasm binary file at '" + wasmBinaryFile + "'"}
                            return response["arrayBuffer"]()
                        }).catch(function () {return getBinary(wasmBinaryFile)})
                    }
                }
                return Promise.resolve().then(function () {return getBinary(wasmBinaryFile)})
            }

            function createWasm() {
                var info = { "env": asmLibraryArg, "wasi_snapshot_preview1": asmLibraryArg };

                function receiveInstance(instance, module) {
                    var exports = instance.exports;
                    Module["asm"] = exports;
                    wasmMemory = Module["asm"]["memory"];
                    updateGlobalBufferAndViews(wasmMemory.buffer);
                    wasmTable = Module["asm"]["__indirect_function_table"];
                    addOnInit(Module["asm"]["__wasm_call_ctors"]);
                    removeRunDependency("wasm-instantiate")
                }

                addRunDependency("wasm-instantiate");

                function receiveInstantiationResult(result) {receiveInstance(result["instance"])}

                function instantiateArrayBuffer(receiver) {
                    return getBinaryPromise().then(function (binary) {
                        var result = WebAssembly.instantiate(binary, info);
                        return result
                    }).then(receiver, function (reason) {
                        err("failed to asynchronously prepare wasm: " + reason);
                        abort(reason)
                    })
                }

                function instantiateAsync() {
                    if (!wasmBinary && typeof WebAssembly.instantiateStreaming === "function" && !isDataURI(wasmBinaryFile) && typeof fetch === "function") {
                        return fetch(wasmBinaryFile, { credentials: "same-origin" }).then(function (response) {
                            var result = WebAssembly.instantiateStreaming(response, info);
                            return result.then(receiveInstantiationResult, function (reason) {
                                err("wasm streaming compile failed: " + reason);
                                err("falling back to ArrayBuffer instantiation");
                                return instantiateArrayBuffer(receiveInstantiationResult)
                            })
                        })
                    } else {return instantiateArrayBuffer(receiveInstantiationResult)}
                }

                if (Module["instantiateWasm"]) {
                    try {
                        var exports = Module["instantiateWasm"](info, receiveInstance);
                        return exports
                    } catch (e) {
                        err("Module.instantiateWasm callback failed with error: " + e);
                        return false
                    }
                }
                instantiateAsync().catch(readyPromiseReject);
                return {}
            }

            var tempDouble;
            var tempI64;
            var ASM_CONSTS = {
                589324: function ($0, $1) {MONO.string_decoder.decode($0, $0 + $1, true)}, 589375: function ($0, $1, $2) {
                    var js_str = MONO.string_decoder.copy($0);
                    try {
                        var res = eval(js_str);
                        setValue($2, 0, "i32");
                        if (res === null || res === undefined) return 0; else res = res.toString()
                    } catch (e) {
                        res = e.toString();
                        setValue($2, 1, "i32");
                        if (res === null || res === undefined) res = "unknown exception";
                        var stack = e.stack;
                        if (stack) {if (stack.startsWith(res)) res = stack; else res += "\n" + stack}
                    }
                    var buff = Module._malloc((res.length + 1) * 2);
                    stringToUTF16(res, buff, (res.length + 1) * 2);
                    setValue($1, res.length, "i32");
                    return buff
                }, 589930: function ($0, $1, $2, $3, $4) {
                    var log_level = $0;
                    var message = Module.UTF8ToString($1);
                    var isFatal = $2;
                    var domain = Module.UTF8ToString($3);
                    var dataPtr = $4;
                    if (MONO["logging"] && MONO.logging["trace"]) {
                        MONO.logging.trace(domain, log_level, message, isFatal, dataPtr);
                        return
                    }
                    if (isFatal) console.trace(message);
                    switch (Module.UTF8ToString($0)) {
                        case"critical":
                        case"error":
                            console.error(message);
                            break;
                        case"warning":
                            console.warn(message);
                            break;
                        case"message":
                            console.log(message);
                            break;
                        case"info":
                            console.info(message);
                            break;
                        case"debug":
                            console.debug(message);
                            break;
                        default:
                            console.log(message);
                            break
                    }
                }, 590554: function ($0, $1) {
                    var level = $0;
                    var message = Module.UTF8ToString($1);
                    var namespace = "Debugger.Debug";
                    if (MONO["logging"] && MONO.logging["debugger"]) {
                        MONO.logging.debugger(level, message);
                        return
                    }
                    console.debug("%s: %s", namespace, message)
                }, 590794: function ($0, $1, $2, $3) {MONO.mono_wasm_add_dbg_command_received($0, $1, $2, $3)}, 590856: function ($0, $1, $2, $3) {MONO.mono_wasm_add_dbg_command_received($0, $1, $2, $3)}, 590918: function ($0, $1) {MONO.mono_wasm_add_dbg_command_received(1, -1, $0, $1)}
            };

            function compile_function(snippet_ptr, len, is_exception) {
                try {
                    var data = MONO.string_decoder.decode(snippet_ptr, snippet_ptr + len);
                    var wrapper = "(function () { " + data + " })";
                    var funcFactory = eval(wrapper);
                    var func = funcFactory();
                    if (typeof func !== "function") {throw new Error("Code must return an instance of a JavaScript function. " + "Please use `return` statement to return a function.")}
                    setValue(is_exception, 0, "i32");
                    return BINDING.js_to_mono_obj(func, true)
                } catch (e) {
                    res = e.toString();
                    setValue(is_exception, 1, "i32");
                    if (res === null || res === undefined) res = "unknown exception";
                    return BINDING.js_to_mono_obj(res, true)
                }
            }

            function callRuntimeCallbacks(callbacks) {
                while (callbacks.length > 0) {
                    var callback = callbacks.shift();
                    if (typeof callback == "function") {
                        callback(Module);
                        continue
                    }
                    var func = callback.func;
                    if (typeof func === "number") {if (callback.arg === undefined) {wasmTable.get(func)()} else {wasmTable.get(func)(callback.arg)}} else {func(callback.arg === undefined ? null : callback.arg)}
                }
            }

            function demangle(func) {return func}

            function demangleAll(text) {
                var regex = /\b_Z[\w\d_]+/g;
                return text.replace(regex, function (x) {
                    var y = demangle(x);
                    return x === y ? x : y + " [" + x + "]"
                })
            }

            function jsStackTrace() {
                var error = new Error;
                if (!error.stack) {
                    try {throw new Error} catch (e) {error = e}
                    if (!error.stack) {return "(no stack trace available)"}
                }
                return error.stack.toString()
            }

            var runtimeKeepaliveCounter = 0;

            function keepRuntimeAlive() {return noExitRuntime || runtimeKeepaliveCounter > 0}

            function ___assert_fail(condition, filename, line, func) {abort("Assertion failed: " + UTF8ToString(condition) + ", at: " + [filename ? UTF8ToString(filename) : "unknown filename", line, func ? UTF8ToString(func) : "unknown function"])}

            var _emscripten_get_now;
            _emscripten_get_now = function () {return 0};
            var _emscripten_get_now_is_monotonic = true;

            function setErrNo(value) {
                HEAP32[___errno_location() >> 2] = value;
                return value
            }

            function _clock_gettime(clk_id, tp) {
                var now;
                if (clk_id === 0) {now = Date.now()} else if ((clk_id === 1 || clk_id === 4) && _emscripten_get_now_is_monotonic) {now = _emscripten_get_now()} else {
                    setErrNo(28);
                    return -1
                }
                HEAP32[tp >> 2] = now / 1e3 | 0;
                HEAP32[tp + 4 >> 2] = now % 1e3 * 1e3 * 1e3 | 0;
                return 0
            }

            function ___clock_gettime(a0, a1) {return _clock_gettime(a0, a1)}

            var ExceptionInfoAttrs = { DESTRUCTOR_OFFSET: 0, REFCOUNT_OFFSET: 4, TYPE_OFFSET: 8, CAUGHT_OFFSET: 12, RETHROWN_OFFSET: 13, SIZE: 16 };

            function ___cxa_allocate_exception(size) {return _malloc(size + ExceptionInfoAttrs.SIZE) + ExceptionInfoAttrs.SIZE}

            function ExceptionInfo(excPtr) {
                this.excPtr = excPtr;
                this.ptr = excPtr - ExceptionInfoAttrs.SIZE;
                this.set_type = function (type) {HEAP32[this.ptr + ExceptionInfoAttrs.TYPE_OFFSET >> 2] = type};
                this.get_type = function () {return HEAP32[this.ptr + ExceptionInfoAttrs.TYPE_OFFSET >> 2]};
                this.set_destructor = function (destructor) {HEAP32[this.ptr + ExceptionInfoAttrs.DESTRUCTOR_OFFSET >> 2] = destructor};
                this.get_destructor = function () {return HEAP32[this.ptr + ExceptionInfoAttrs.DESTRUCTOR_OFFSET >> 2]};
                this.set_refcount = function (refcount) {HEAP32[this.ptr + ExceptionInfoAttrs.REFCOUNT_OFFSET >> 2] = refcount};
                this.set_caught = function (caught) {
                    caught = caught ? 1 : 0;
                    HEAP8[this.ptr + ExceptionInfoAttrs.CAUGHT_OFFSET >> 0] = caught
                };
                this.get_caught = function () {return HEAP8[this.ptr + ExceptionInfoAttrs.CAUGHT_OFFSET >> 0] != 0};
                this.set_rethrown = function (rethrown) {
                    rethrown = rethrown ? 1 : 0;
                    HEAP8[this.ptr + ExceptionInfoAttrs.RETHROWN_OFFSET >> 0] = rethrown
                };
                this.get_rethrown = function () {return HEAP8[this.ptr + ExceptionInfoAttrs.RETHROWN_OFFSET >> 0] != 0};
                this.init = function (type, destructor) {
                    this.set_type(type);
                    this.set_destructor(destructor);
                    this.set_refcount(0);
                    this.set_caught(false);
                    this.set_rethrown(false)
                };
                this.add_ref = function () {
                    var value = HEAP32[this.ptr + ExceptionInfoAttrs.REFCOUNT_OFFSET >> 2];
                    HEAP32[this.ptr + ExceptionInfoAttrs.REFCOUNT_OFFSET >> 2] = value + 1
                };
                this.release_ref = function () {
                    var prev = HEAP32[this.ptr + ExceptionInfoAttrs.REFCOUNT_OFFSET >> 2];
                    HEAP32[this.ptr + ExceptionInfoAttrs.REFCOUNT_OFFSET >> 2] = prev - 1;
                    return prev === 1
                }
            }

            function CatchInfo(ptr) {
                this.free = function () {
                    _free(this.ptr);
                    this.ptr = 0
                };
                this.set_base_ptr = function (basePtr) {HEAP32[this.ptr >> 2] = basePtr};
                this.get_base_ptr = function () {return HEAP32[this.ptr >> 2]};
                this.set_adjusted_ptr = function (adjustedPtr) {
                    var ptrSize = 4;
                    HEAP32[this.ptr + ptrSize >> 2] = adjustedPtr
                };
                this.get_adjusted_ptr = function () {
                    var ptrSize = 4;
                    return HEAP32[this.ptr + ptrSize >> 2]
                };
                this.get_exception_ptr = function () {
                    var isPointer = ___cxa_is_pointer_type(this.get_exception_info().get_type());
                    if (isPointer) {return HEAP32[this.get_base_ptr() >> 2]}
                    var adjusted = this.get_adjusted_ptr();
                    if (adjusted !== 0) return adjusted;
                    return this.get_base_ptr()
                };
                this.get_exception_info = function () {return new ExceptionInfo(this.get_base_ptr())};
                if (ptr === undefined) {
                    this.ptr = _malloc(8);
                    this.set_adjusted_ptr(0)
                } else {this.ptr = ptr}
            }

            var exceptionCaught = [];

            function exception_addRef(info) {info.add_ref()}

            var uncaughtExceptionCount = 0;

            function ___cxa_begin_catch(ptr) {
                var catchInfo = new CatchInfo(ptr);
                var info = catchInfo.get_exception_info();
                if (!info.get_caught()) {
                    info.set_caught(true);
                    uncaughtExceptionCount--
                }
                info.set_rethrown(false);
                exceptionCaught.push(catchInfo);
                exception_addRef(info);
                return catchInfo.get_exception_ptr()
            }

            var exceptionLast = 0;

            function ___cxa_free_exception(ptr) {return _free(new ExceptionInfo(ptr).ptr)}

            function exception_decRef(info) {
                if (info.release_ref() && !info.get_rethrown()) {
                    var destructor = info.get_destructor();
                    if (destructor) {wasmTable.get(destructor)(info.excPtr)}
                    ___cxa_free_exception(info.excPtr)
                }
            }

            function ___cxa_end_catch() {
                _setThrew(0);
                var catchInfo = exceptionCaught.pop();
                exception_decRef(catchInfo.get_exception_info());
                catchInfo.free();
                exceptionLast = 0
            }

            function ___resumeException(catchInfoPtr) {
                var catchInfo = new CatchInfo(catchInfoPtr);
                var ptr = catchInfo.get_base_ptr();
                if (!exceptionLast) {exceptionLast = ptr}
                catchInfo.free();
                throw ptr
            }

            function ___cxa_find_matching_catch_3() {
                var thrown = exceptionLast;
                if (!thrown) {
                    setTempRet0(0);
                    return 0 | 0
                }
                var info = new ExceptionInfo(thrown);
                var thrownType = info.get_type();
                var catchInfo = new CatchInfo;
                catchInfo.set_base_ptr(thrown);
                if (!thrownType) {
                    setTempRet0(0);
                    return catchInfo.ptr | 0
                }
                var typeArray = Array.prototype.slice.call(arguments);
                var stackTop = stackSave();
                var exceptionThrowBuf = stackAlloc(4);
                HEAP32[exceptionThrowBuf >> 2] = thrown;
                for (var i = 0; i < typeArray.length; i++) {
                    var caughtType = typeArray[i];
                    if (caughtType === 0 || caughtType === thrownType) {break}
                    if (___cxa_can_catch(caughtType, thrownType, exceptionThrowBuf)) {
                        var adjusted = HEAP32[exceptionThrowBuf >> 2];
                        if (thrown !== adjusted) {catchInfo.set_adjusted_ptr(adjusted)}
                        setTempRet0(caughtType);
                        return catchInfo.ptr | 0
                    }
                }
                stackRestore(stackTop);
                setTempRet0(thrownType);
                return catchInfo.ptr | 0
            }

            function ___cxa_throw(ptr, type, destructor) {
                var info = new ExceptionInfo(ptr);
                info.init(type, destructor);
                exceptionLast = ptr;
                uncaughtExceptionCount++;
                throw ptr
            }

            var PATH = {
                splitPath: function (filename) {
                    var splitPathRe = /^(\/?|)([\s\S]*?)((?:\.{1,2}|[^\/]+?|)(\.[^.\/]*|))(?:[\/]*)$/;
                    return splitPathRe.exec(filename).slice(1)
                }, normalizeArray: function (parts, allowAboveRoot) {
                    var up = 0;
                    for (var i = parts.length - 1; i >= 0; i--) {
                        var last = parts[i];
                        if (last === ".") {parts.splice(i, 1)} else if (last === "..") {
                            parts.splice(i, 1);
                            up++
                        } else if (up) {
                            parts.splice(i, 1);
                            up--
                        }
                    }
                    if (allowAboveRoot) {for (; up; up--) {parts.unshift("..")}}
                    return parts
                }, normalize: function (path) {
                    var isAbsolute = path.charAt(0) === "/",
                        trailingSlash = path.substr(-1) === "/";
                    path = PATH.normalizeArray(path.split("/").filter(function (p) {return !!p}), !isAbsolute).join("/");
                    if (!path && !isAbsolute) {path = "."}
                    if (path && trailingSlash) {path += "/"}
                    return (isAbsolute ? "/" : "") + path
                }, dirname: function (path) {
                    var result = PATH.splitPath(path),
                        root = result[0],
                        dir = result[1];
                    if (!root && !dir) {return "."}
                    if (dir) {dir = dir.substr(0, dir.length - 1)}
                    return root + dir
                }, basename: function (path) {
                    if (path === "/") return "/";
                    path = PATH.normalize(path);
                    path = path.replace(/\/$/, "");
                    var lastSlash = path.lastIndexOf("/");
                    if (lastSlash === -1) return path;
                    return path.substr(lastSlash + 1)
                }, extname: function (path) {return PATH.splitPath(path)[3]}, join: function () {
                    var paths = Array.prototype.slice.call(arguments, 0);
                    return PATH.normalize(paths.join("/"))
                }, join2: function (l, r) {return PATH.normalize(l + "/" + r)}
            };

            function getRandomDevice() {
                if (typeof crypto === "object" && typeof crypto["getRandomValues"] === "function") {
                    var randomBuffer = new Uint8Array(1);
                    return function () {
                        crypto.getRandomValues(randomBuffer);
                        return randomBuffer[0]
                    }
                } else return function () {abort("randomDevice")}
            }

            var PATH_FS = {
                resolve: function () {
                    var resolvedPath = "",
                        resolvedAbsolute = false;
                    for (var i = arguments.length - 1; i >= -1 && !resolvedAbsolute; i--) {
                        var path = i >= 0 ? arguments[i] : FS.cwd();
                        if (typeof path !== "string") {throw new TypeError("Arguments to path.resolve must be strings")} else if (!path) {return ""}
                        resolvedPath = path + "/" + resolvedPath;
                        resolvedAbsolute = path.charAt(0) === "/"
                    }
                    resolvedPath = PATH.normalizeArray(resolvedPath.split("/").filter(function (p) {return !!p}), !resolvedAbsolute).join("/");
                    return (resolvedAbsolute ? "/" : "") + resolvedPath || "."
                }, relative: function (from, to) {
                    from = PATH_FS.resolve(from).substr(1);
                    to = PATH_FS.resolve(to).substr(1);

                    function trim(arr) {
                        var start = 0;
                        for (; start < arr.length; start++) {if (arr[start] !== "") break}
                        var end = arr.length - 1;
                        for (; end >= 0; end--) {if (arr[end] !== "") break}
                        if (start > end) return [];
                        return arr.slice(start, end - start + 1)
                    }

                    var fromParts = trim(from.split("/"));
                    var toParts = trim(to.split("/"));
                    var length = Math.min(fromParts.length, toParts.length);
                    var samePartsLength = length;
                    for (var i = 0; i < length; i++) {
                        if (fromParts[i] !== toParts[i]) {
                            samePartsLength = i;
                            break
                        }
                    }
                    var outputParts = [];
                    for (var i = samePartsLength; i < fromParts.length; i++) {outputParts.push("..")}
                    outputParts = outputParts.concat(toParts.slice(samePartsLength));
                    return outputParts.join("/")
                }
            };
            var TTY = {
                ttys: [], init: function () {}, shutdown: function () {}, register: function (dev, ops) {
                    TTY.ttys[dev] = { input: [], output: [], ops: ops };
                    FS.registerDevice(dev, TTY.stream_ops)
                }, stream_ops: {
                    open: function (stream) {
                        var tty = TTY.ttys[stream.node.rdev];
                        if (!tty) {throw new FS.ErrnoError(43)}
                        stream.tty = tty;
                        stream.seekable = false
                    }, close: function (stream) {stream.tty.ops.flush(stream.tty)}, flush: function (stream) {stream.tty.ops.flush(stream.tty)}, read: function (stream, buffer, offset, length, pos) {
                        if (!stream.tty || !stream.tty.ops.get_char) {throw new FS.ErrnoError(60)}
                        var bytesRead = 0;
                        for (var i = 0; i < length; i++) {
                            var result;
                            try {result = stream.tty.ops.get_char(stream.tty)} catch (e) {throw new FS.ErrnoError(29)}
                            if (result === undefined && bytesRead === 0) {throw new FS.ErrnoError(6)}
                            if (result === null || result === undefined) break;
                            bytesRead++;
                            buffer[offset + i] = result
                        }
                        if (bytesRead) {stream.node.timestamp = Date.now()}
                        return bytesRead
                    }, write: function (stream, buffer, offset, length, pos) {
                        if (!stream.tty || !stream.tty.ops.put_char) {throw new FS.ErrnoError(60)}
                        try {for (var i = 0; i < length; i++) {stream.tty.ops.put_char(stream.tty, buffer[offset + i])}} catch (e) {throw new FS.ErrnoError(29)}
                        if (length) {stream.node.timestamp = Date.now()}
                        return i
                    }
                }, default_tty_ops: {
                    get_char: function (tty) {
                        if (!tty.input.length) {
                            var result = null;
                            if (typeof window != "undefined" && typeof window.prompt == "function") {
                                result = window.prompt("Input: ");
                                if (result !== null) {result += "\n"}
                            } else if (typeof readline == "function") {
                                result = readline();
                                if (result !== null) {result += "\n"}
                            }
                            if (!result) {return null}
                            tty.input = intArrayFromString(result, true)
                        }
                        return tty.input.shift()
                    }, put_char: function (tty, val) {
                        if (val === null || val === 10) {
                            out(UTF8ArrayToString(tty.output, 0));
                            tty.output = []
                        } else {if (val != 0) tty.output.push(val)}
                    }, flush: function (tty) {
                        if (tty.output && tty.output.length > 0) {
                            out(UTF8ArrayToString(tty.output, 0));
                            tty.output = []
                        }
                    }
                }, default_tty1_ops: {
                    put_char: function (tty, val) {
                        if (val === null || val === 10) {
                            err(UTF8ArrayToString(tty.output, 0));
                            tty.output = []
                        } else {if (val != 0) tty.output.push(val)}
                    }, flush: function (tty) {
                        if (tty.output && tty.output.length > 0) {
                            err(UTF8ArrayToString(tty.output, 0));
                            tty.output = []
                        }
                    }
                }
            };

            function mmapAlloc(size) {
                var alignedSize = alignMemory(size, 65536);
                var ptr = _malloc(alignedSize);
                while (size < alignedSize) HEAP8[ptr + size++] = 0;
                return ptr
            }

            var MEMFS = {
                ops_table: null, mount: function (mount) {return MEMFS.createNode(null, "/", 16384 | 511, 0)}, createNode: function (parent, name, mode, dev) {
                    if (FS.isBlkdev(mode) || FS.isFIFO(mode)) {throw new FS.ErrnoError(63)}
                    if (!MEMFS.ops_table) {MEMFS.ops_table = { dir: { node: { getattr: MEMFS.node_ops.getattr, setattr: MEMFS.node_ops.setattr, lookup: MEMFS.node_ops.lookup, mknod: MEMFS.node_ops.mknod, rename: MEMFS.node_ops.rename, unlink: MEMFS.node_ops.unlink, rmdir: MEMFS.node_ops.rmdir, readdir: MEMFS.node_ops.readdir, symlink: MEMFS.node_ops.symlink }, stream: { llseek: MEMFS.stream_ops.llseek } }, file: { node: { getattr: MEMFS.node_ops.getattr, setattr: MEMFS.node_ops.setattr }, stream: { llseek: MEMFS.stream_ops.llseek, read: MEMFS.stream_ops.read, write: MEMFS.stream_ops.write, allocate: MEMFS.stream_ops.allocate, mmap: MEMFS.stream_ops.mmap, msync: MEMFS.stream_ops.msync } }, link: { node: { getattr: MEMFS.node_ops.getattr, setattr: MEMFS.node_ops.setattr, readlink: MEMFS.node_ops.readlink }, stream: {} }, chrdev: { node: { getattr: MEMFS.node_ops.getattr, setattr: MEMFS.node_ops.setattr }, stream: FS.chrdev_stream_ops } }}
                    var node = FS.createNode(parent, name, mode, dev);
                    if (FS.isDir(node.mode)) {
                        node.node_ops = MEMFS.ops_table.dir.node;
                        node.stream_ops = MEMFS.ops_table.dir.stream;
                        node.contents = {}
                    } else if (FS.isFile(node.mode)) {
                        node.node_ops = MEMFS.ops_table.file.node;
                        node.stream_ops = MEMFS.ops_table.file.stream;
                        node.usedBytes = 0;
                        node.contents = null
                    } else if (FS.isLink(node.mode)) {
                        node.node_ops = MEMFS.ops_table.link.node;
                        node.stream_ops = MEMFS.ops_table.link.stream
                    } else if (FS.isChrdev(node.mode)) {
                        node.node_ops = MEMFS.ops_table.chrdev.node;
                        node.stream_ops = MEMFS.ops_table.chrdev.stream
                    }
                    node.timestamp = Date.now();
                    if (parent) {
                        parent.contents[name] = node;
                        parent.timestamp = node.timestamp
                    }
                    return node
                }, getFileDataAsTypedArray: function (node) {
                    if (!node.contents) return new Uint8Array(0);
                    if (node.contents.subarray) return node.contents.subarray(0, node.usedBytes);
                    return new Uint8Array(node.contents)
                }, expandFileStorage: function (node, newCapacity) {
                    var prevCapacity = node.contents ? node.contents.length : 0;
                    if (prevCapacity >= newCapacity) return;
                    var CAPACITY_DOUBLING_MAX = 1024 * 1024;
                    newCapacity = Math.max(newCapacity, prevCapacity * (prevCapacity < CAPACITY_DOUBLING_MAX ? 2 : 1.125) >>> 0);
                    if (prevCapacity != 0) newCapacity = Math.max(newCapacity, 256);
                    var oldContents = node.contents;
                    node.contents = new Uint8Array(newCapacity);
                    if (node.usedBytes > 0) node.contents.set(oldContents.subarray(0, node.usedBytes), 0)
                }, resizeFileStorage: function (node, newSize) {
                    if (node.usedBytes == newSize) return;
                    if (newSize == 0) {
                        node.contents = null;
                        node.usedBytes = 0
                    } else {
                        var oldContents = node.contents;
                        node.contents = new Uint8Array(newSize);
                        if (oldContents) {node.contents.set(oldContents.subarray(0, Math.min(newSize, node.usedBytes)))}
                        node.usedBytes = newSize
                    }
                }, node_ops: {
                    getattr: function (node) {
                        var attr = {};
                        attr.dev = FS.isChrdev(node.mode) ? node.id : 1;
                        attr.ino = node.id;
                        attr.mode = node.mode;
                        attr.nlink = 1;
                        attr.uid = 0;
                        attr.gid = 0;
                        attr.rdev = node.rdev;
                        if (FS.isDir(node.mode)) {attr.size = 4096} else if (FS.isFile(node.mode)) {attr.size = node.usedBytes} else if (FS.isLink(node.mode)) {attr.size = node.link.length} else {attr.size = 0}
                        attr.atime = new Date(node.timestamp);
                        attr.mtime = new Date(node.timestamp);
                        attr.ctime = new Date(node.timestamp);
                        attr.blksize = 4096;
                        attr.blocks = Math.ceil(attr.size / attr.blksize);
                        return attr
                    }, setattr: function (node, attr) {
                        if (attr.mode !== undefined) {node.mode = attr.mode}
                        if (attr.timestamp !== undefined) {node.timestamp = attr.timestamp}
                        if (attr.size !== undefined) {MEMFS.resizeFileStorage(node, attr.size)}
                    }, lookup: function (parent, name) {throw FS.genericErrors[44]}, mknod: function (parent, name, mode, dev) {return MEMFS.createNode(parent, name, mode, dev)}, rename: function (old_node, new_dir, new_name) {
                        if (FS.isDir(old_node.mode)) {
                            var new_node;
                            try {new_node = FS.lookupNode(new_dir, new_name)} catch (e) {}
                            if (new_node) {for (var i in new_node.contents) {throw new FS.ErrnoError(55)}}
                        }
                        delete old_node.parent.contents[old_node.name];
                        old_node.parent.timestamp = Date.now();
                        old_node.name = new_name;
                        new_dir.contents[new_name] = old_node;
                        new_dir.timestamp = old_node.parent.timestamp;
                        old_node.parent = new_dir
                    }, unlink: function (parent, name) {
                        delete parent.contents[name];
                        parent.timestamp = Date.now()
                    }, rmdir: function (parent, name) {
                        var node = FS.lookupNode(parent, name);
                        for (var i in node.contents) {throw new FS.ErrnoError(55)}
                        delete parent.contents[name];
                        parent.timestamp = Date.now()
                    }, readdir: function (node) {
                        var entries = [".", ".."];
                        for (var key in node.contents) {
                            if (!node.contents.hasOwnProperty(key)) {continue}
                            entries.push(key)
                        }
                        return entries
                    }, symlink: function (parent, newname, oldpath) {
                        var node = MEMFS.createNode(parent, newname, 511 | 40960, 0);
                        node.link = oldpath;
                        return node
                    }, readlink: function (node) {
                        if (!FS.isLink(node.mode)) {throw new FS.ErrnoError(28)}
                        return node.link
                    }
                }, stream_ops: {
                    read: function (stream, buffer, offset, length, position) {
                        var contents = stream.node.contents;
                        if (position >= stream.node.usedBytes) return 0;
                        var size = Math.min(stream.node.usedBytes - position, length);
                        if (size > 8 && contents.subarray) {buffer.set(contents.subarray(position, position + size), offset)} else {for (var i = 0; i < size; i++) buffer[offset + i] = contents[position + i]}
                        return size
                    }, write: function (stream, buffer, offset, length, position, canOwn) {
                        if (buffer.buffer === HEAP8.buffer) {canOwn = false}
                        if (!length) return 0;
                        var node = stream.node;
                        node.timestamp = Date.now();
                        if (buffer.subarray && (!node.contents || node.contents.subarray)) {
                            if (canOwn) {
                                node.contents = buffer.subarray(offset, offset + length);
                                node.usedBytes = length;
                                return length
                            } else if (node.usedBytes === 0 && position === 0) {
                                node.contents = buffer.slice(offset, offset + length);
                                node.usedBytes = length;
                                return length
                            } else if (position + length <= node.usedBytes) {
                                node.contents.set(buffer.subarray(offset, offset + length), position);
                                return length
                            }
                        }
                        MEMFS.expandFileStorage(node, position + length);
                        if (node.contents.subarray && buffer.subarray) {node.contents.set(buffer.subarray(offset, offset + length), position)} else {for (var i = 0; i < length; i++) {node.contents[position + i] = buffer[offset + i]}}
                        node.usedBytes = Math.max(node.usedBytes, position + length);
                        return length
                    }, llseek: function (stream, offset, whence) {
                        var position = offset;
                        if (whence === 1) {position += stream.position} else if (whence === 2) {if (FS.isFile(stream.node.mode)) {position += stream.node.usedBytes}}
                        if (position < 0) {throw new FS.ErrnoError(28)}
                        return position
                    }, allocate: function (stream, offset, length) {
                        MEMFS.expandFileStorage(stream.node, offset + length);
                        stream.node.usedBytes = Math.max(stream.node.usedBytes, offset + length)
                    }, mmap: function (stream, address, length, position, prot, flags) {
                        if (address !== 0) {throw new FS.ErrnoError(28)}
                        if (!FS.isFile(stream.node.mode)) {throw new FS.ErrnoError(43)}
                        var ptr;
                        var allocated;
                        var contents = stream.node.contents;
                        if (!(flags & 2) && contents.buffer === buffer) {
                            allocated = false;
                            ptr = contents.byteOffset
                        } else {
                            if (position > 0 || position + length < contents.length) {if (contents.subarray) {contents = contents.subarray(position, position + length)} else {contents = Array.prototype.slice.call(contents, position, position + length)}}
                            allocated = true;
                            ptr = mmapAlloc(length);
                            if (!ptr) {throw new FS.ErrnoError(48)}
                            HEAP8.set(contents, ptr)
                        }
                        return { ptr: ptr, allocated: allocated }
                    }, msync: function (stream, buffer, offset, length, mmapFlags) {
                        if (!FS.isFile(stream.node.mode)) {throw new FS.ErrnoError(43)}
                        if (mmapFlags & 2) {return 0}
                        var bytesWritten = MEMFS.stream_ops.write(stream, buffer, 0, length, offset, false);
                        return 0
                    }
                }
            };
            var FS = {
                root: null, mounts: [], devices: {}, streams: [], nextInode: 1, nameTable: null, currentPath: "/", initialized: false, ignorePermissions: true, trackingDelegate: {}, tracking: { openFlags: { READ: 1, WRITE: 2 } }, ErrnoError: null, genericErrors: {}, filesystems: null, syncFSRequests: 0, lookupPath: function (path, opts) {
                    path = PATH_FS.resolve(FS.cwd(), path);
                    opts = opts || {};
                    if (!path) return { path: "", node: null };
                    var defaults = { follow_mount: true, recurse_count: 0 };
                    for (var key in defaults) {if (opts[key] === undefined) {opts[key] = defaults[key]}}
                    if (opts.recurse_count > 8) {throw new FS.ErrnoError(32)}
                    var parts = PATH.normalizeArray(path.split("/").filter(function (p) {return !!p}), false);
                    var current = FS.root;
                    var current_path = "/";
                    for (var i = 0; i < parts.length; i++) {
                        var islast = i === parts.length - 1;
                        if (islast && opts.parent) {break}
                        current = FS.lookupNode(current, parts[i]);
                        current_path = PATH.join2(current_path, parts[i]);
                        if (FS.isMountpoint(current)) {if (!islast || islast && opts.follow_mount) {current = current.mounted.root}}
                        if (!islast || opts.follow) {
                            var count = 0;
                            while (FS.isLink(current.mode)) {
                                var link = FS.readlink(current_path);
                                current_path = PATH_FS.resolve(PATH.dirname(current_path), link);
                                var lookup = FS.lookupPath(current_path, { recurse_count: opts.recurse_count });
                                current = lookup.node;
                                if (count++ > 40) {throw new FS.ErrnoError(32)}
                            }
                        }
                    }
                    return { path: current_path, node: current }
                }, getPath: function (node) {
                    var path;
                    while (true) {
                        if (FS.isRoot(node)) {
                            var mount = node.mount.mountpoint;
                            if (!path) return mount;
                            return mount[mount.length - 1] !== "/" ? mount + "/" + path : mount + path
                        }
                        path = path ? node.name + "/" + path : node.name;
                        node = node.parent
                    }
                }, hashName: function (parentid, name) {
                    var hash = 0;
                    for (var i = 0; i < name.length; i++) {hash = (hash << 5) - hash + name.charCodeAt(i) | 0}
                    return (parentid + hash >>> 0) % FS.nameTable.length
                }, hashAddNode: function (node) {
                    var hash = FS.hashName(node.parent.id, node.name);
                    node.name_next = FS.nameTable[hash];
                    FS.nameTable[hash] = node
                }, hashRemoveNode: function (node) {
                    var hash = FS.hashName(node.parent.id, node.name);
                    if (FS.nameTable[hash] === node) {FS.nameTable[hash] = node.name_next} else {
                        var current = FS.nameTable[hash];
                        while (current) {
                            if (current.name_next === node) {
                                current.name_next = node.name_next;
                                break
                            }
                            current = current.name_next
                        }
                    }
                }, lookupNode: function (parent, name) {
                    var errCode = FS.mayLookup(parent);
                    if (errCode) {throw new FS.ErrnoError(errCode, parent)}
                    var hash = FS.hashName(parent.id, name);
                    for (var node = FS.nameTable[hash]; node; node = node.name_next) {
                        var nodeName = node.name;
                        if (node.parent.id === parent.id && nodeName === name) {return node}
                    }
                    return FS.lookup(parent, name)
                }, createNode: function (parent, name, mode, rdev) {
                    var node = new FS.FSNode(parent, name, mode, rdev);
                    FS.hashAddNode(node);
                    return node
                }, destroyNode: function (node) {FS.hashRemoveNode(node)}, isRoot: function (node) {return node === node.parent}, isMountpoint: function (node) {return !!node.mounted}, isFile: function (mode) {return (mode & 61440) === 32768}, isDir: function (mode) {return (mode & 61440) === 16384}, isLink: function (mode) {return (mode & 61440) === 40960}, isChrdev: function (mode) {return (mode & 61440) === 8192}, isBlkdev: function (mode) {return (mode & 61440) === 24576}, isFIFO: function (mode) {return (mode & 61440) === 4096}, isSocket: function (mode) {return (mode & 49152) === 49152}, flagModes: { "r": 0, "r+": 2, "w": 577, "w+": 578, "a": 1089, "a+": 1090 }, modeStringToFlags: function (str) {
                    var flags = FS.flagModes[str];
                    if (typeof flags === "undefined") {throw new Error("Unknown file open mode: " + str)}
                    return flags
                }, flagsToPermissionString: function (flag) {
                    var perms = ["r", "w", "rw"][flag & 3];
                    if (flag & 512) {perms += "w"}
                    return perms
                }, nodePermissions: function (node, perms) {
                    if (FS.ignorePermissions) {return 0}
                    if (perms.includes("r") && !(node.mode & 292)) {return 2} else if (perms.includes("w") && !(node.mode & 146)) {return 2} else if (perms.includes("x") && !(node.mode & 73)) {return 2}
                    return 0
                }, mayLookup: function (dir) {
                    var errCode = FS.nodePermissions(dir, "x");
                    if (errCode) return errCode;
                    if (!dir.node_ops.lookup) return 2;
                    return 0
                }, mayCreate: function (dir, name) {
                    try {
                        var node = FS.lookupNode(dir, name);
                        return 20
                    } catch (e) {}
                    return FS.nodePermissions(dir, "wx")
                }, mayDelete: function (dir, name, isdir) {
                    var node;
                    try {node = FS.lookupNode(dir, name)} catch (e) {return e.errno}
                    var errCode = FS.nodePermissions(dir, "wx");
                    if (errCode) {return errCode}
                    if (isdir) {
                        if (!FS.isDir(node.mode)) {return 54}
                        if (FS.isRoot(node) || FS.getPath(node) === FS.cwd()) {return 10}
                    } else {if (FS.isDir(node.mode)) {return 31}}
                    return 0
                }, mayOpen: function (node, flags) {
                    if (!node) {return 44}
                    if (FS.isLink(node.mode)) {return 32} else if (FS.isDir(node.mode)) {if (FS.flagsToPermissionString(flags) !== "r" || flags & 512) {return 31}}
                    return FS.nodePermissions(node, FS.flagsToPermissionString(flags))
                }, MAX_OPEN_FDS: 4096, nextfd: function (fd_start, fd_end) {
                    fd_start = fd_start || 0;
                    fd_end = fd_end || FS.MAX_OPEN_FDS;
                    for (var fd = fd_start; fd <= fd_end; fd++) {if (!FS.streams[fd]) {return fd}}
                    throw new FS.ErrnoError(33)
                }, getStream: function (fd) {return FS.streams[fd]}, createStream: function (stream, fd_start, fd_end) {
                    if (!FS.FSStream) {
                        FS.FSStream = function () {};
                        FS.FSStream.prototype = { object: { get: function () {return this.node}, set: function (val) {this.node = val} }, isRead: { get: function () {return (this.flags & 2097155) !== 1} }, isWrite: { get: function () {return (this.flags & 2097155) !== 0} }, isAppend: { get: function () {return this.flags & 1024} } }
                    }
                    var newStream = new FS.FSStream;
                    for (var p in stream) {newStream[p] = stream[p]}
                    stream = newStream;
                    var fd = FS.nextfd(fd_start, fd_end);
                    stream.fd = fd;
                    FS.streams[fd] = stream;
                    return stream
                }, closeStream: function (fd) {FS.streams[fd] = null}, chrdev_stream_ops: {
                    open: function (stream) {
                        var device = FS.getDevice(stream.node.rdev);
                        stream.stream_ops = device.stream_ops;
                        if (stream.stream_ops.open) {stream.stream_ops.open(stream)}
                    }, llseek: function () {throw new FS.ErrnoError(70)}
                }, major: function (dev) {return dev >> 8}, minor: function (dev) {return dev & 255}, makedev: function (ma, mi) {return ma << 8 | mi}, registerDevice: function (dev, ops) {FS.devices[dev] = { stream_ops: ops }}, getDevice: function (dev) {return FS.devices[dev]}, getMounts: function (mount) {
                    var mounts = [];
                    var check = [mount];
                    while (check.length) {
                        var m = check.pop();
                        mounts.push(m);
                        check.push.apply(check, m.mounts)
                    }
                    return mounts
                }, syncfs: function (populate, callback) {
                    if (typeof populate === "function") {
                        callback = populate;
                        populate = false
                    }
                    FS.syncFSRequests++;
                    if (FS.syncFSRequests > 1) {err("warning: " + FS.syncFSRequests + " FS.syncfs operations in flight at once, probably just doing extra work")}
                    var mounts = FS.getMounts(FS.root.mount);
                    var completed = 0;

                    function doCallback(errCode) {
                        FS.syncFSRequests--;
                        return callback(errCode)
                    }

                    function done(errCode) {
                        if (errCode) {
                            if (!done.errored) {
                                done.errored = true;
                                return doCallback(errCode)
                            }
                            return
                        }
                        if (++completed >= mounts.length) {doCallback(null)}
                    }

                    mounts.forEach(function (mount) {
                        if (!mount.type.syncfs) {return done(null)}
                        mount.type.syncfs(mount, populate, done)
                    })
                }, mount: function (type, opts, mountpoint) {
                    var root = mountpoint === "/";
                    var pseudo = !mountpoint;
                    var node;
                    if (root && FS.root) {throw new FS.ErrnoError(10)} else if (!root && !pseudo) {
                        var lookup = FS.lookupPath(mountpoint, { follow_mount: false });
                        mountpoint = lookup.path;
                        node = lookup.node;
                        if (FS.isMountpoint(node)) {throw new FS.ErrnoError(10)}
                        if (!FS.isDir(node.mode)) {throw new FS.ErrnoError(54)}
                    }
                    var mount = { type: type, opts: opts, mountpoint: mountpoint, mounts: [] };
                    var mountRoot = type.mount(mount);
                    mountRoot.mount = mount;
                    mount.root = mountRoot;
                    if (root) {FS.root = mountRoot} else if (node) {
                        node.mounted = mount;
                        if (node.mount) {node.mount.mounts.push(mount)}
                    }
                    return mountRoot
                }, unmount: function (mountpoint) {
                    var lookup = FS.lookupPath(mountpoint, { follow_mount: false });
                    if (!FS.isMountpoint(lookup.node)) {throw new FS.ErrnoError(28)}
                    var node = lookup.node;
                    var mount = node.mounted;
                    var mounts = FS.getMounts(mount);
                    Object.keys(FS.nameTable).forEach(function (hash) {
                        var current = FS.nameTable[hash];
                        while (current) {
                            var next = current.name_next;
                            if (mounts.includes(current.mount)) {FS.destroyNode(current)}
                            current = next
                        }
                    });
                    node.mounted = null;
                    var idx = node.mount.mounts.indexOf(mount);
                    node.mount.mounts.splice(idx, 1)
                }, lookup: function (parent, name) {return parent.node_ops.lookup(parent, name)}, mknod: function (path, mode, dev) {
                    var lookup = FS.lookupPath(path, { parent: true });
                    var parent = lookup.node;
                    var name = PATH.basename(path);
                    if (!name || name === "." || name === "..") {throw new FS.ErrnoError(28)}
                    var errCode = FS.mayCreate(parent, name);
                    if (errCode) {throw new FS.ErrnoError(errCode)}
                    if (!parent.node_ops.mknod) {throw new FS.ErrnoError(63)}
                    return parent.node_ops.mknod(parent, name, mode, dev)
                }, create: function (path, mode) {
                    mode = mode !== undefined ? mode : 438;
                    mode &= 4095;
                    mode |= 32768;
                    return FS.mknod(path, mode, 0)
                }, mkdir: function (path, mode) {
                    mode = mode !== undefined ? mode : 511;
                    mode &= 511 | 512;
                    mode |= 16384;
                    return FS.mknod(path, mode, 0)
                }, mkdirTree: function (path, mode) {
                    var dirs = path.split("/");
                    var d = "";
                    for (var i = 0; i < dirs.length; ++i) {
                        if (!dirs[i]) continue;
                        d += "/" + dirs[i];
                        try {FS.mkdir(d, mode)} catch (e) {if (e.errno != 20) throw e}
                    }
                }, mkdev: function (path, mode, dev) {
                    if (typeof dev === "undefined") {
                        dev = mode;
                        mode = 438
                    }
                    mode |= 8192;
                    return FS.mknod(path, mode, dev)
                }, symlink: function (oldpath, newpath) {
                    if (!PATH_FS.resolve(oldpath)) {throw new FS.ErrnoError(44)}
                    var lookup = FS.lookupPath(newpath, { parent: true });
                    var parent = lookup.node;
                    if (!parent) {throw new FS.ErrnoError(44)}
                    var newname = PATH.basename(newpath);
                    var errCode = FS.mayCreate(parent, newname);
                    if (errCode) {throw new FS.ErrnoError(errCode)}
                    if (!parent.node_ops.symlink) {throw new FS.ErrnoError(63)}
                    return parent.node_ops.symlink(parent, newname, oldpath)
                }, rename: function (old_path, new_path) {
                    var old_dirname = PATH.dirname(old_path);
                    var new_dirname = PATH.dirname(new_path);
                    var old_name = PATH.basename(old_path);
                    var new_name = PATH.basename(new_path);
                    var lookup,
                        old_dir,
                        new_dir;
                    lookup = FS.lookupPath(old_path, { parent: true });
                    old_dir = lookup.node;
                    lookup = FS.lookupPath(new_path, { parent: true });
                    new_dir = lookup.node;
                    if (!old_dir || !new_dir) throw new FS.ErrnoError(44);
                    if (old_dir.mount !== new_dir.mount) {throw new FS.ErrnoError(75)}
                    var old_node = FS.lookupNode(old_dir, old_name);
                    var relative = PATH_FS.relative(old_path, new_dirname);
                    if (relative.charAt(0) !== ".") {throw new FS.ErrnoError(28)}
                    relative = PATH_FS.relative(new_path, old_dirname);
                    if (relative.charAt(0) !== ".") {throw new FS.ErrnoError(55)}
                    var new_node;
                    try {new_node = FS.lookupNode(new_dir, new_name)} catch (e) {}
                    if (old_node === new_node) {return}
                    var isdir = FS.isDir(old_node.mode);
                    var errCode = FS.mayDelete(old_dir, old_name, isdir);
                    if (errCode) {throw new FS.ErrnoError(errCode)}
                    errCode = new_node ? FS.mayDelete(new_dir, new_name, isdir) : FS.mayCreate(new_dir, new_name);
                    if (errCode) {throw new FS.ErrnoError(errCode)}
                    if (!old_dir.node_ops.rename) {throw new FS.ErrnoError(63)}
                    if (FS.isMountpoint(old_node) || new_node && FS.isMountpoint(new_node)) {throw new FS.ErrnoError(10)}
                    if (new_dir !== old_dir) {
                        errCode = FS.nodePermissions(old_dir, "w");
                        if (errCode) {throw new FS.ErrnoError(errCode)}
                    }
                    try {if (FS.trackingDelegate["willMovePath"]) {FS.trackingDelegate["willMovePath"](old_path, new_path)}} catch (e) {err("FS.trackingDelegate['willMovePath']('" + old_path + "', '" + new_path + "') threw an exception: " + e.message)}
                    FS.hashRemoveNode(old_node);
                    try {old_dir.node_ops.rename(old_node, new_dir, new_name)} catch (e) {throw e} finally {FS.hashAddNode(old_node)}
                    try {if (FS.trackingDelegate["onMovePath"]) FS.trackingDelegate["onMovePath"](old_path, new_path)} catch (e) {err("FS.trackingDelegate['onMovePath']('" + old_path + "', '" + new_path + "') threw an exception: " + e.message)}
                }, rmdir: function (path) {
                    var lookup = FS.lookupPath(path, { parent: true });
                    var parent = lookup.node;
                    var name = PATH.basename(path);
                    var node = FS.lookupNode(parent, name);
                    var errCode = FS.mayDelete(parent, name, true);
                    if (errCode) {throw new FS.ErrnoError(errCode)}
                    if (!parent.node_ops.rmdir) {throw new FS.ErrnoError(63)}
                    if (FS.isMountpoint(node)) {throw new FS.ErrnoError(10)}
                    try {if (FS.trackingDelegate["willDeletePath"]) {FS.trackingDelegate["willDeletePath"](path)}} catch (e) {err("FS.trackingDelegate['willDeletePath']('" + path + "') threw an exception: " + e.message)}
                    parent.node_ops.rmdir(parent, name);
                    FS.destroyNode(node);
                    try {if (FS.trackingDelegate["onDeletePath"]) FS.trackingDelegate["onDeletePath"](path)} catch (e) {err("FS.trackingDelegate['onDeletePath']('" + path + "') threw an exception: " + e.message)}
                }, readdir: function (path) {
                    var lookup = FS.lookupPath(path, { follow: true });
                    var node = lookup.node;
                    if (!node.node_ops.readdir) {throw new FS.ErrnoError(54)}
                    return node.node_ops.readdir(node)
                }, unlink: function (path) {
                    var lookup = FS.lookupPath(path, { parent: true });
                    var parent = lookup.node;
                    var name = PATH.basename(path);
                    var node = FS.lookupNode(parent, name);
                    var errCode = FS.mayDelete(parent, name, false);
                    if (errCode) {throw new FS.ErrnoError(errCode)}
                    if (!parent.node_ops.unlink) {throw new FS.ErrnoError(63)}
                    if (FS.isMountpoint(node)) {throw new FS.ErrnoError(10)}
                    try {if (FS.trackingDelegate["willDeletePath"]) {FS.trackingDelegate["willDeletePath"](path)}} catch (e) {err("FS.trackingDelegate['willDeletePath']('" + path + "') threw an exception: " + e.message)}
                    parent.node_ops.unlink(parent, name);
                    FS.destroyNode(node);
                    try {if (FS.trackingDelegate["onDeletePath"]) FS.trackingDelegate["onDeletePath"](path)} catch (e) {err("FS.trackingDelegate['onDeletePath']('" + path + "') threw an exception: " + e.message)}
                }, readlink: function (path) {
                    var lookup = FS.lookupPath(path);
                    var link = lookup.node;
                    if (!link) {throw new FS.ErrnoError(44)}
                    if (!link.node_ops.readlink) {throw new FS.ErrnoError(28)}
                    return PATH_FS.resolve(FS.getPath(link.parent), link.node_ops.readlink(link))
                }, stat: function (path, dontFollow) {
                    var lookup = FS.lookupPath(path, { follow: !dontFollow });
                    var node = lookup.node;
                    if (!node) {throw new FS.ErrnoError(44)}
                    if (!node.node_ops.getattr) {throw new FS.ErrnoError(63)}
                    return node.node_ops.getattr(node)
                }, lstat: function (path) {return FS.stat(path, true)}, chmod: function (path, mode, dontFollow) {
                    var node;
                    if (typeof path === "string") {
                        var lookup = FS.lookupPath(path, { follow: !dontFollow });
                        node = lookup.node
                    } else {node = path}
                    if (!node.node_ops.setattr) {throw new FS.ErrnoError(63)}
                    node.node_ops.setattr(node, { mode: mode & 4095 | node.mode & ~4095, timestamp: Date.now() })
                }, lchmod: function (path, mode) {FS.chmod(path, mode, true)}, fchmod: function (fd, mode) {
                    var stream = FS.getStream(fd);
                    if (!stream) {throw new FS.ErrnoError(8)}
                    FS.chmod(stream.node, mode)
                }, chown: function (path, uid, gid, dontFollow) {
                    var node;
                    if (typeof path === "string") {
                        var lookup = FS.lookupPath(path, { follow: !dontFollow });
                        node = lookup.node
                    } else {node = path}
                    if (!node.node_ops.setattr) {throw new FS.ErrnoError(63)}
                    node.node_ops.setattr(node, { timestamp: Date.now() })
                }, lchown: function (path, uid, gid) {FS.chown(path, uid, gid, true)}, fchown: function (fd, uid, gid) {
                    var stream = FS.getStream(fd);
                    if (!stream) {throw new FS.ErrnoError(8)}
                    FS.chown(stream.node, uid, gid)
                }, truncate: function (path, len) {
                    if (len < 0) {throw new FS.ErrnoError(28)}
                    var node;
                    if (typeof path === "string") {
                        var lookup = FS.lookupPath(path, { follow: true });
                        node = lookup.node
                    } else {node = path}
                    if (!node.node_ops.setattr) {throw new FS.ErrnoError(63)}
                    if (FS.isDir(node.mode)) {throw new FS.ErrnoError(31)}
                    if (!FS.isFile(node.mode)) {throw new FS.ErrnoError(28)}
                    var errCode = FS.nodePermissions(node, "w");
                    if (errCode) {throw new FS.ErrnoError(errCode)}
                    node.node_ops.setattr(node, { size: len, timestamp: Date.now() })
                }, ftruncate: function (fd, len) {
                    var stream = FS.getStream(fd);
                    if (!stream) {throw new FS.ErrnoError(8)}
                    if ((stream.flags & 2097155) === 0) {throw new FS.ErrnoError(28)}
                    FS.truncate(stream.node, len)
                }, utime: function (path, atime, mtime) {
                    var lookup = FS.lookupPath(path, { follow: true });
                    var node = lookup.node;
                    node.node_ops.setattr(node, { timestamp: Math.max(atime, mtime) })
                }, open: function (path, flags, mode, fd_start, fd_end) {
                    if (path === "") {throw new FS.ErrnoError(44)}
                    flags = typeof flags === "string" ? FS.modeStringToFlags(flags) : flags;
                    mode = typeof mode === "undefined" ? 438 : mode;
                    if (flags & 64) {mode = mode & 4095 | 32768} else {mode = 0}
                    var node;
                    if (typeof path === "object") {node = path} else {
                        path = PATH.normalize(path);
                        try {
                            var lookup = FS.lookupPath(path, { follow: !(flags & 131072) });
                            node = lookup.node
                        } catch (e) {}
                    }
                    var created = false;
                    if (flags & 64) {
                        if (node) {if (flags & 128) {throw new FS.ErrnoError(20)}} else {
                            node = FS.mknod(path, mode, 0);
                            created = true
                        }
                    }
                    if (!node) {throw new FS.ErrnoError(44)}
                    if (FS.isChrdev(node.mode)) {flags &= ~512}
                    if (flags & 65536 && !FS.isDir(node.mode)) {throw new FS.ErrnoError(54)}
                    if (!created) {
                        var errCode = FS.mayOpen(node, flags);
                        if (errCode) {throw new FS.ErrnoError(errCode)}
                    }
                    if (flags & 512) {FS.truncate(node, 0)}
                    flags &= ~(128 | 512 | 131072);
                    var stream = FS.createStream({ node: node, path: FS.getPath(node), flags: flags, seekable: true, position: 0, stream_ops: node.stream_ops, ungotten: [], error: false }, fd_start, fd_end);
                    if (stream.stream_ops.open) {stream.stream_ops.open(stream)}
                    if (Module["logReadFiles"] && !(flags & 1)) {
                        if (!FS.readFiles) FS.readFiles = {};
                        if (!(path in FS.readFiles)) {
                            FS.readFiles[path] = 1;
                            err("FS.trackingDelegate error on read file: " + path)
                        }
                    }
                    try {
                        if (FS.trackingDelegate["onOpenFile"]) {
                            var trackingFlags = 0;
                            if ((flags & 2097155) !== 1) {trackingFlags |= FS.tracking.openFlags.READ}
                            if ((flags & 2097155) !== 0) {trackingFlags |= FS.tracking.openFlags.WRITE}
                            FS.trackingDelegate["onOpenFile"](path, trackingFlags)
                        }
                    } catch (e) {err("FS.trackingDelegate['onOpenFile']('" + path + "', flags) threw an exception: " + e.message)}
                    return stream
                }, close: function (stream) {
                    if (FS.isClosed(stream)) {throw new FS.ErrnoError(8)}
                    if (stream.getdents) stream.getdents = null;
                    try {if (stream.stream_ops.close) {stream.stream_ops.close(stream)}} catch (e) {throw e} finally {FS.closeStream(stream.fd)}
                    stream.fd = null
                }, isClosed: function (stream) {return stream.fd === null}, llseek: function (stream, offset, whence) {
                    if (FS.isClosed(stream)) {throw new FS.ErrnoError(8)}
                    if (!stream.seekable || !stream.stream_ops.llseek) {throw new FS.ErrnoError(70)}
                    if (whence != 0 && whence != 1 && whence != 2) {throw new FS.ErrnoError(28)}
                    stream.position = stream.stream_ops.llseek(stream, offset, whence);
                    stream.ungotten = [];
                    return stream.position
                }, read: function (stream, buffer, offset, length, position) {
                    if (length < 0 || position < 0) {throw new FS.ErrnoError(28)}
                    if (FS.isClosed(stream)) {throw new FS.ErrnoError(8)}
                    if ((stream.flags & 2097155) === 1) {throw new FS.ErrnoError(8)}
                    if (FS.isDir(stream.node.mode)) {throw new FS.ErrnoError(31)}
                    if (!stream.stream_ops.read) {throw new FS.ErrnoError(28)}
                    var seeking = typeof position !== "undefined";
                    if (!seeking) {position = stream.position} else if (!stream.seekable) {throw new FS.ErrnoError(70)}
                    var bytesRead = stream.stream_ops.read(stream, buffer, offset, length, position);
                    if (!seeking) stream.position += bytesRead;
                    return bytesRead
                }, write: function (stream, buffer, offset, length, position, canOwn) {
                    if (length < 0 || position < 0) {throw new FS.ErrnoError(28)}
                    if (FS.isClosed(stream)) {throw new FS.ErrnoError(8)}
                    if ((stream.flags & 2097155) === 0) {throw new FS.ErrnoError(8)}
                    if (FS.isDir(stream.node.mode)) {throw new FS.ErrnoError(31)}
                    if (!stream.stream_ops.write) {throw new FS.ErrnoError(28)}
                    if (stream.seekable && stream.flags & 1024) {FS.llseek(stream, 0, 2)}
                    var seeking = typeof position !== "undefined";
                    if (!seeking) {position = stream.position} else if (!stream.seekable) {throw new FS.ErrnoError(70)}
                    var bytesWritten = stream.stream_ops.write(stream, buffer, offset, length, position, canOwn);
                    if (!seeking) stream.position += bytesWritten;
                    try {if (stream.path && FS.trackingDelegate["onWriteToFile"]) FS.trackingDelegate["onWriteToFile"](stream.path)} catch (e) {err("FS.trackingDelegate['onWriteToFile']('" + stream.path + "') threw an exception: " + e.message)}
                    return bytesWritten
                }, allocate: function (stream, offset, length) {
                    if (FS.isClosed(stream)) {throw new FS.ErrnoError(8)}
                    if (offset < 0 || length <= 0) {throw new FS.ErrnoError(28)}
                    if ((stream.flags & 2097155) === 0) {throw new FS.ErrnoError(8)}
                    if (!FS.isFile(stream.node.mode) && !FS.isDir(stream.node.mode)) {throw new FS.ErrnoError(43)}
                    if (!stream.stream_ops.allocate) {throw new FS.ErrnoError(138)}
                    stream.stream_ops.allocate(stream, offset, length)
                }, mmap: function (stream, address, length, position, prot, flags) {
                    if ((prot & 2) !== 0 && (flags & 2) === 0 && (stream.flags & 2097155) !== 2) {throw new FS.ErrnoError(2)}
                    if ((stream.flags & 2097155) === 1) {throw new FS.ErrnoError(2)}
                    if (!stream.stream_ops.mmap) {throw new FS.ErrnoError(43)}
                    return stream.stream_ops.mmap(stream, address, length, position, prot, flags)
                }, msync: function (stream, buffer, offset, length, mmapFlags) {
                    if (!stream || !stream.stream_ops.msync) {return 0}
                    return stream.stream_ops.msync(stream, buffer, offset, length, mmapFlags)
                }, munmap: function (stream) {return 0}, ioctl: function (stream, cmd, arg) {
                    if (!stream.stream_ops.ioctl) {throw new FS.ErrnoError(59)}
                    return stream.stream_ops.ioctl(stream, cmd, arg)
                }, readFile: function (path, opts) {
                    opts = opts || {};
                    opts.flags = opts.flags || 0;
                    opts.encoding = opts.encoding || "binary";
                    if (opts.encoding !== "utf8" && opts.encoding !== "binary") {throw new Error('Invalid encoding type "' + opts.encoding + '"')}
                    var ret;
                    var stream = FS.open(path, opts.flags);
                    var stat = FS.stat(path);
                    var length = stat.size;
                    var buf = new Uint8Array(length);
                    FS.read(stream, buf, 0, length, 0);
                    if (opts.encoding === "utf8") {ret = UTF8ArrayToString(buf, 0)} else if (opts.encoding === "binary") {ret = buf}
                    FS.close(stream);
                    return ret
                }, writeFile: function (path, data, opts) {
                    opts = opts || {};
                    opts.flags = opts.flags || 577;
                    var stream = FS.open(path, opts.flags, opts.mode);
                    if (typeof data === "string") {
                        var buf = new Uint8Array(lengthBytesUTF8(data) + 1);
                        var actualNumBytes = stringToUTF8Array(data, buf, 0, buf.length);
                        FS.write(stream, buf, 0, actualNumBytes, undefined, opts.canOwn)
                    } else if (ArrayBuffer.isView(data)) {FS.write(stream, data, 0, data.byteLength, undefined, opts.canOwn)} else {throw new Error("Unsupported data type")}
                    FS.close(stream)
                }, cwd: function () {return FS.currentPath}, chdir: function (path) {
                    var lookup = FS.lookupPath(path, { follow: true });
                    if (lookup.node === null) {throw new FS.ErrnoError(44)}
                    if (!FS.isDir(lookup.node.mode)) {throw new FS.ErrnoError(54)}
                    var errCode = FS.nodePermissions(lookup.node, "x");
                    if (errCode) {throw new FS.ErrnoError(errCode)}
                    FS.currentPath = lookup.path
                }, createDefaultDirectories: function () {
                    FS.mkdir("/tmp");
                    FS.mkdir("/home");
                    FS.mkdir("/home/web_user")
                }, createDefaultDevices: function () {
                    FS.mkdir("/dev");
                    FS.registerDevice(FS.makedev(1, 3), { read: function () {return 0}, write: function (stream, buffer, offset, length, pos) {return length} });
                    FS.mkdev("/dev/null", FS.makedev(1, 3));
                    TTY.register(FS.makedev(5, 0), TTY.default_tty_ops);
                    TTY.register(FS.makedev(6, 0), TTY.default_tty1_ops);
                    FS.mkdev("/dev/tty", FS.makedev(5, 0));
                    FS.mkdev("/dev/tty1", FS.makedev(6, 0));
                    var random_device = getRandomDevice();
                    FS.createDevice("/dev", "random", random_device);
                    FS.createDevice("/dev", "urandom", random_device);
                    FS.mkdir("/dev/shm");
                    FS.mkdir("/dev/shm/tmp")
                }, createSpecialDirectories: function () {
                    FS.mkdir("/proc");
                    var proc_self = FS.mkdir("/proc/self");
                    FS.mkdir("/proc/self/fd");
                    FS.mount({
                        mount: function () {
                            var node = FS.createNode(proc_self, "fd", 16384 | 511, 73);
                            node.node_ops = {
                                lookup: function (parent, name) {
                                    var fd = +name;
                                    var stream = FS.getStream(fd);
                                    if (!stream) throw new FS.ErrnoError(8);
                                    var ret = { parent: null, mount: { mountpoint: "fake" }, node_ops: { readlink: function () {return stream.path} } };
                                    ret.parent = ret;
                                    return ret
                                }
                            };
                            return node
                        }
                    }, {}, "/proc/self/fd")
                }, createStandardStreams: function () {
                    if (Module["stdin"]) {FS.createDevice("/dev", "stdin", Module["stdin"])} else {FS.symlink("/dev/tty", "/dev/stdin")}
                    if (Module["stdout"]) {FS.createDevice("/dev", "stdout", null, Module["stdout"])} else {FS.symlink("/dev/tty", "/dev/stdout")}
                    if (Module["stderr"]) {FS.createDevice("/dev", "stderr", null, Module["stderr"])} else {FS.symlink("/dev/tty1", "/dev/stderr")}
                    var stdin = FS.open("/dev/stdin", 0);
                    var stdout = FS.open("/dev/stdout", 1);
                    var stderr = FS.open("/dev/stderr", 1)
                }, ensureErrnoError: function () {
                    if (FS.ErrnoError) return;
                    FS.ErrnoError = function ErrnoError(errno, node) {
                        this.node = node;
                        this.setErrno = function (errno) {this.errno = errno};
                        this.setErrno(errno);
                        this.message = "FS error"
                    };
                    FS.ErrnoError.prototype = new Error;
                    FS.ErrnoError.prototype.constructor = FS.ErrnoError;
                    [44].forEach(function (code) {
                        FS.genericErrors[code] = new FS.ErrnoError(code);
                        FS.genericErrors[code].stack = "<generic error, no stack>"
                    })
                }, staticInit: function () {
                    FS.ensureErrnoError();
                    FS.nameTable = new Array(4096);
                    FS.mount(MEMFS, {}, "/");
                    FS.createDefaultDirectories();
                    FS.createDefaultDevices();
                    FS.createSpecialDirectories();
                    FS.filesystems = { "MEMFS": MEMFS }
                }, init: function (input, output, error) {
                    FS.init.initialized = true;
                    FS.ensureErrnoError();
                    Module["stdin"] = input || Module["stdin"];
                    Module["stdout"] = output || Module["stdout"];
                    Module["stderr"] = error || Module["stderr"];
                    FS.createStandardStreams()
                }, quit: function () {
                    FS.init.initialized = false;
                    var fflush = Module["_fflush"];
                    if (fflush) fflush(0);
                    for (var i = 0; i < FS.streams.length; i++) {
                        var stream = FS.streams[i];
                        if (!stream) {continue}
                        FS.close(stream)
                    }
                }, getMode: function (canRead, canWrite) {
                    var mode = 0;
                    if (canRead) mode |= 292 | 73;
                    if (canWrite) mode |= 146;
                    return mode
                }, findObject: function (path, dontResolveLastLink) {
                    var ret = FS.analyzePath(path, dontResolveLastLink);
                    if (ret.exists) {return ret.object} else {return null}
                }, analyzePath: function (path, dontResolveLastLink) {
                    try {
                        var lookup = FS.lookupPath(path, { follow: !dontResolveLastLink });
                        path = lookup.path
                    } catch (e) {}
                    var ret = { isRoot: false, exists: false, error: 0, name: null, path: null, object: null, parentExists: false, parentPath: null, parentObject: null };
                    try {
                        var lookup = FS.lookupPath(path, { parent: true });
                        ret.parentExists = true;
                        ret.parentPath = lookup.path;
                        ret.parentObject = lookup.node;
                        ret.name = PATH.basename(path);
                        lookup = FS.lookupPath(path, { follow: !dontResolveLastLink });
                        ret.exists = true;
                        ret.path = lookup.path;
                        ret.object = lookup.node;
                        ret.name = lookup.node.name;
                        ret.isRoot = lookup.path === "/"
                    } catch (e) {ret.error = e.errno}
                    return ret
                }, createPath: function (parent, path, canRead, canWrite) {
                    parent = typeof parent === "string" ? parent : FS.getPath(parent);
                    var parts = path.split("/").reverse();
                    while (parts.length) {
                        var part = parts.pop();
                        if (!part) continue;
                        var current = PATH.join2(parent, part);
                        try {FS.mkdir(current)} catch (e) {}
                        parent = current
                    }
                    return current
                }, createFile: function (parent, name, properties, canRead, canWrite) {
                    var path = PATH.join2(typeof parent === "string" ? parent : FS.getPath(parent), name);
                    var mode = FS.getMode(canRead, canWrite);
                    return FS.create(path, mode)
                }, createDataFile: function (parent, name, data, canRead, canWrite, canOwn) {
                    var path = name ? PATH.join2(typeof parent === "string" ? parent : FS.getPath(parent), name) : parent;
                    var mode = FS.getMode(canRead, canWrite);
                    var node = FS.create(path, mode);
                    if (data) {
                        if (typeof data === "string") {
                            var arr = new Array(data.length);
                            for (var i = 0, len = data.length; i < len; ++i) arr[i] = data.charCodeAt(i);
                            data = arr
                        }
                        FS.chmod(node, mode | 146);
                        var stream = FS.open(node, 577);
                        FS.write(stream, data, 0, data.length, 0, canOwn);
                        FS.close(stream);
                        FS.chmod(node, mode)
                    }
                    return node
                }, createDevice: function (parent, name, input, output) {
                    var path = PATH.join2(typeof parent === "string" ? parent : FS.getPath(parent), name);
                    var mode = FS.getMode(!!input, !!output);
                    if (!FS.createDevice.major) FS.createDevice.major = 64;
                    var dev = FS.makedev(FS.createDevice.major++, 0);
                    FS.registerDevice(dev, {
                        open: function (stream) {stream.seekable = false}, close: function (stream) {if (output && output.buffer && output.buffer.length) {output(10)}}, read: function (stream, buffer, offset, length, pos) {
                            var bytesRead = 0;
                            for (var i = 0; i < length; i++) {
                                var result;
                                try {result = input()} catch (e) {throw new FS.ErrnoError(29)}
                                if (result === undefined && bytesRead === 0) {throw new FS.ErrnoError(6)}
                                if (result === null || result === undefined) break;
                                bytesRead++;
                                buffer[offset + i] = result
                            }
                            if (bytesRead) {stream.node.timestamp = Date.now()}
                            return bytesRead
                        }, write: function (stream, buffer, offset, length, pos) {
                            for (var i = 0; i < length; i++) {try {output(buffer[offset + i])} catch (e) {throw new FS.ErrnoError(29)}}
                            if (length) {stream.node.timestamp = Date.now()}
                            return i
                        }
                    });
                    return FS.mkdev(path, mode, dev)
                }, forceLoadFile: function (obj) {
                    if (obj.isDevice || obj.isFolder || obj.link || obj.contents) return true;
                    if (typeof XMLHttpRequest !== "undefined") {throw new Error("Lazy loading should have been performed (contents set) in createLazyFile, but it was not. Lazy loading only works in web workers. Use --embed-file or --preload-file in emcc on the main thread.")} else if (read_) {
                        try {
                            obj.contents = intArrayFromString(read_(obj.url), true);
                            obj.usedBytes = obj.contents.length
                        } catch (e) {throw new FS.ErrnoError(29)}
                    } else {throw new Error("Cannot load without read() or XMLHttpRequest.")}
                }, createLazyFile: function (parent, name, url, canRead, canWrite) {
                    function LazyUint8Array() {
                        this.lengthKnown = false;
                        this.chunks = []
                    }

                    LazyUint8Array.prototype.get = function LazyUint8Array_get(idx) {
                        if (idx > this.length - 1 || idx < 0) {return undefined}
                        var chunkOffset = idx % this.chunkSize;
                        var chunkNum = idx / this.chunkSize | 0;
                        return this.getter(chunkNum)[chunkOffset]
                    };
                    LazyUint8Array.prototype.setDataGetter = function LazyUint8Array_setDataGetter(getter) {this.getter = getter};
                    LazyUint8Array.prototype.cacheLength = function LazyUint8Array_cacheLength() {
                        var xhr = new XMLHttpRequest;
                        xhr.open("HEAD", url, false);
                        xhr.send(null);
                        if (!(xhr.status >= 200 && xhr.status < 300 || xhr.status === 304)) throw new Error("Couldn't load " + url + ". Status: " + xhr.status);
                        var datalength = Number(xhr.getResponseHeader("Content-length"));
                        var header;
                        var hasByteServing = (header = xhr.getResponseHeader("Accept-Ranges")) && header === "bytes";
                        var usesGzip = (header = xhr.getResponseHeader("Content-Encoding")) && header === "gzip";
                        var chunkSize = 1024 * 1024;
                        if (!hasByteServing) chunkSize = datalength;
                        var doXHR = function (from, to) {
                            if (from > to) throw new Error("invalid range (" + from + ", " + to + ") or no bytes requested!");
                            if (to > datalength - 1) throw new Error("only " + datalength + " bytes available! programmer error!");
                            var xhr = new XMLHttpRequest;
                            xhr.open("GET", url, false);
                            if (datalength !== chunkSize) xhr.setRequestHeader("Range", "bytes=" + from + "-" + to);
                            if (typeof Uint8Array != "undefined") xhr.responseType = "arraybuffer";
                            if (xhr.overrideMimeType) {xhr.overrideMimeType("text/plain; charset=x-user-defined")}
                            xhr.send(null);
                            if (!(xhr.status >= 200 && xhr.status < 300 || xhr.status === 304)) throw new Error("Couldn't load " + url + ". Status: " + xhr.status);
                            if (xhr.response !== undefined) {return new Uint8Array(xhr.response || [])} else {return intArrayFromString(xhr.responseText || "", true)}
                        };
                        var lazyArray = this;
                        lazyArray.setDataGetter(function (chunkNum) {
                            var start = chunkNum * chunkSize;
                            var end = (chunkNum + 1) * chunkSize - 1;
                            end = Math.min(end, datalength - 1);
                            if (typeof lazyArray.chunks[chunkNum] === "undefined") {lazyArray.chunks[chunkNum] = doXHR(start, end)}
                            if (typeof lazyArray.chunks[chunkNum] === "undefined") throw new Error("doXHR failed!");
                            return lazyArray.chunks[chunkNum]
                        });
                        if (usesGzip || !datalength) {
                            chunkSize = datalength = 1;
                            datalength = this.getter(0).length;
                            chunkSize = datalength;
                            out("LazyFiles on gzip forces download of the whole file when length is accessed")
                        }
                        this._length = datalength;
                        this._chunkSize = chunkSize;
                        this.lengthKnown = true
                    };
                    if (typeof XMLHttpRequest !== "undefined") {
                        if (!ENVIRONMENT_IS_WORKER) throw"Cannot do synchronous binary XHRs outside webworkers in modern browsers. Use --embed-file or --preload-file in emcc";
                        var lazyArray = new LazyUint8Array;
                        Object.defineProperties(lazyArray, {
                            length: {
                                get: function () {
                                    if (!this.lengthKnown) {this.cacheLength()}
                                    return this._length
                                }
                            }, chunkSize: {
                                get: function () {
                                    if (!this.lengthKnown) {this.cacheLength()}
                                    return this._chunkSize
                                }
                            }
                        });
                        var properties = { isDevice: false, contents: lazyArray }
                    } else {var properties = { isDevice: false, url: url }}
                    var node = FS.createFile(parent, name, properties, canRead, canWrite);
                    if (properties.contents) {node.contents = properties.contents} else if (properties.url) {
                        node.contents = null;
                        node.url = properties.url
                    }
                    Object.defineProperties(node, { usedBytes: { get: function () {return this.contents.length} } });
                    var stream_ops = {};
                    var keys = Object.keys(node.stream_ops);
                    keys.forEach(function (key) {
                        var fn = node.stream_ops[key];
                        stream_ops[key] = function forceLoadLazyFile() {
                            FS.forceLoadFile(node);
                            return fn.apply(null, arguments)
                        }
                    });
                    stream_ops.read = function stream_ops_read(stream, buffer, offset, length, position) {
                        FS.forceLoadFile(node);
                        var contents = stream.node.contents;
                        if (position >= contents.length) return 0;
                        var size = Math.min(contents.length - position, length);
                        if (contents.slice) {for (var i = 0; i < size; i++) {buffer[offset + i] = contents[position + i]}} else {for (var i = 0; i < size; i++) {buffer[offset + i] = contents.get(position + i)}}
                        return size
                    };
                    node.stream_ops = stream_ops;
                    return node
                }, createPreloadedFile: function (parent, name, url, canRead, canWrite, onload, onerror, dontCreateFile, canOwn, preFinish) {
                    Browser.init();
                    var fullname = name ? PATH_FS.resolve(PATH.join2(parent, name)) : parent;
                    var dep = getUniqueRunDependency("cp " + fullname);

                    function processData(byteArray) {
                        function finish(byteArray) {
                            if (preFinish) preFinish();
                            if (!dontCreateFile) {FS.createDataFile(parent, name, byteArray, canRead, canWrite, canOwn)}
                            if (onload) onload();
                            removeRunDependency(dep)
                        }

                        var handled = false;
                        Module["preloadPlugins"].forEach(function (plugin) {
                            if (handled) return;
                            if (plugin["canHandle"](fullname)) {
                                plugin["handle"](byteArray, fullname, finish, function () {
                                    if (onerror) onerror();
                                    removeRunDependency(dep)
                                });
                                handled = true
                            }
                        });
                        if (!handled) finish(byteArray)
                    }

                    addRunDependency(dep);
                    if (typeof url == "string") {Browser.asyncLoad(url, function (byteArray) {processData(byteArray)}, onerror)} else {processData(url)}
                }, indexedDB: function () {return window.indexedDB || window.mozIndexedDB || window.webkitIndexedDB || window.msIndexedDB}, DB_NAME: function () {return "EM_FS_" + window.location.pathname}, DB_VERSION: 20, DB_STORE_NAME: "FILE_DATA", saveFilesToDB: function (paths, onload, onerror) {
                    onload = onload || function () {};
                    onerror = onerror || function () {};
                    var indexedDB = FS.indexedDB();
                    try {var openRequest = indexedDB.open(FS.DB_NAME(), FS.DB_VERSION)} catch (e) {return onerror(e)}
                    openRequest.onupgradeneeded = function openRequest_onupgradeneeded() {
                        out("creating db");
                        var db = openRequest.result;
                        db.createObjectStore(FS.DB_STORE_NAME)
                    };
                    openRequest.onsuccess = function openRequest_onsuccess() {
                        var db = openRequest.result;
                        var transaction = db.transaction([FS.DB_STORE_NAME], "readwrite");
                        var files = transaction.objectStore(FS.DB_STORE_NAME);
                        var ok = 0,
                            fail = 0,
                            total = paths.length;

                        function finish() {if (fail == 0) onload(); else onerror()}

                        paths.forEach(function (path) {
                            var putRequest = files.put(FS.analyzePath(path).object.contents, path);
                            putRequest.onsuccess = function putRequest_onsuccess() {
                                ok++;
                                if (ok + fail == total) finish()
                            };
                            putRequest.onerror = function putRequest_onerror() {
                                fail++;
                                if (ok + fail == total) finish()
                            }
                        });
                        transaction.onerror = onerror
                    };
                    openRequest.onerror = onerror
                }, loadFilesFromDB: function (paths, onload, onerror) {
                    onload = onload || function () {};
                    onerror = onerror || function () {};
                    var indexedDB = FS.indexedDB();
                    try {var openRequest = indexedDB.open(FS.DB_NAME(), FS.DB_VERSION)} catch (e) {return onerror(e)}
                    openRequest.onupgradeneeded = onerror;
                    openRequest.onsuccess = function openRequest_onsuccess() {
                        var db = openRequest.result;
                        try {var transaction = db.transaction([FS.DB_STORE_NAME], "readonly")} catch (e) {
                            onerror(e);
                            return
                        }
                        var files = transaction.objectStore(FS.DB_STORE_NAME);
                        var ok = 0,
                            fail = 0,
                            total = paths.length;

                        function finish() {if (fail == 0) onload(); else onerror()}

                        paths.forEach(function (path) {
                            var getRequest = files.get(path);
                            getRequest.onsuccess = function getRequest_onsuccess() {
                                if (FS.analyzePath(path).exists) {FS.unlink(path)}
                                FS.createDataFile(PATH.dirname(path), PATH.basename(path), getRequest.result, true, true, true);
                                ok++;
                                if (ok + fail == total) finish()
                            };
                            getRequest.onerror = function getRequest_onerror() {
                                fail++;
                                if (ok + fail == total) finish()
                            }
                        });
                        transaction.onerror = onerror
                    };
                    openRequest.onerror = onerror
                }
            };
            var SYSCALLS = {
                mappings: {}, DEFAULT_POLLMASK: 5, umask: 511, calculateAt: function (dirfd, path, allowEmpty) {
                    if (path[0] === "/") {return path}
                    var dir;
                    if (dirfd === -100) {dir = FS.cwd()} else {
                        var dirstream = FS.getStream(dirfd);
                        if (!dirstream) throw new FS.ErrnoError(8);
                        dir = dirstream.path
                    }
                    if (path.length == 0) {
                        if (!allowEmpty) {throw new FS.ErrnoError(44)}
                        return dir
                    }
                    return PATH.join2(dir, path)
                }, doStat: function (func, path, buf) {
                    try {var stat = func(path)} catch (e) {
                        if (e && e.node && PATH.normalize(path) !== PATH.normalize(FS.getPath(e.node))) {return -54}
                        throw e
                    }
                    HEAP32[buf >> 2] = stat.dev;
                    HEAP32[buf + 4 >> 2] = 0;
                    HEAP32[buf + 8 >> 2] = stat.ino;
                    HEAP32[buf + 12 >> 2] = stat.mode;
                    HEAP32[buf + 16 >> 2] = stat.nlink;
                    HEAP32[buf + 20 >> 2] = stat.uid;
                    HEAP32[buf + 24 >> 2] = stat.gid;
                    HEAP32[buf + 28 >> 2] = stat.rdev;
                    HEAP32[buf + 32 >> 2] = 0;
                    tempI64 = [stat.size >>> 0, (tempDouble = stat.size, +Math.abs(tempDouble) >= 1 ? tempDouble > 0 ? (Math.min(+Math.floor(tempDouble / 4294967296), 4294967295) | 0) >>> 0 : ~~+Math.ceil((tempDouble - +(~~tempDouble >>> 0)) / 4294967296) >>> 0 : 0)], HEAP32[buf + 40 >> 2] = tempI64[0], HEAP32[buf + 44 >> 2] = tempI64[1];
                    HEAP32[buf + 48 >> 2] = 4096;
                    HEAP32[buf + 52 >> 2] = stat.blocks;
                    HEAP32[buf + 56 >> 2] = stat.atime.getTime() / 1e3 | 0;
                    HEAP32[buf + 60 >> 2] = 0;
                    HEAP32[buf + 64 >> 2] = stat.mtime.getTime() / 1e3 | 0;
                    HEAP32[buf + 68 >> 2] = 0;
                    HEAP32[buf + 72 >> 2] = stat.ctime.getTime() / 1e3 | 0;
                    HEAP32[buf + 76 >> 2] = 0;
                    tempI64 = [stat.ino >>> 0, (tempDouble = stat.ino, +Math.abs(tempDouble) >= 1 ? tempDouble > 0 ? (Math.min(+Math.floor(tempDouble / 4294967296), 4294967295) | 0) >>> 0 : ~~+Math.ceil((tempDouble - +(~~tempDouble >>> 0)) / 4294967296) >>> 0 : 0)], HEAP32[buf + 80 >> 2] = tempI64[0], HEAP32[buf + 84 >> 2] = tempI64[1];
                    return 0
                }, doMsync: function (addr, stream, len, flags, offset) {
                    var buffer = HEAPU8.slice(addr, addr + len);
                    FS.msync(stream, buffer, offset, len, flags)
                }, doMkdir: function (path, mode) {
                    path = PATH.normalize(path);
                    if (path[path.length - 1] === "/") path = path.substr(0, path.length - 1);
                    FS.mkdir(path, mode, 0);
                    return 0
                }, doMknod: function (path, mode, dev) {
                    switch (mode & 61440) {
                        case 32768:
                        case 8192:
                        case 24576:
                        case 4096:
                        case 49152:
                            break;
                        default:
                            return -28
                    }
                    FS.mknod(path, mode, dev);
                    return 0
                }, doReadlink: function (path, buf, bufsize) {
                    if (bufsize <= 0) return -28;
                    var ret = FS.readlink(path);
                    var len = Math.min(bufsize, lengthBytesUTF8(ret));
                    var endChar = HEAP8[buf + len];
                    stringToUTF8(ret, buf, bufsize + 1);
                    HEAP8[buf + len] = endChar;
                    return len
                }, doAccess: function (path, amode) {
                    if (amode & ~7) {return -28}
                    var node;
                    var lookup = FS.lookupPath(path, { follow: true });
                    node = lookup.node;
                    if (!node) {return -44}
                    var perms = "";
                    if (amode & 4) perms += "r";
                    if (amode & 2) perms += "w";
                    if (amode & 1) perms += "x";
                    if (perms && FS.nodePermissions(node, perms)) {return -2}
                    return 0
                }, doDup: function (path, flags, suggestFD) {
                    var suggest = FS.getStream(suggestFD);
                    if (suggest) FS.close(suggest);
                    return FS.open(path, flags, 0, suggestFD, suggestFD).fd
                }, doReadv: function (stream, iov, iovcnt, offset) {
                    var ret = 0;
                    for (var i = 0; i < iovcnt; i++) {
                        var ptr = HEAP32[iov + i * 8 >> 2];
                        var len = HEAP32[iov + (i * 8 + 4) >> 2];
                        var curr = FS.read(stream, HEAP8, ptr, len, offset);
                        if (curr < 0) return -1;
                        ret += curr;
                        if (curr < len) break
                    }
                    return ret
                }, doWritev: function (stream, iov, iovcnt, offset) {
                    var ret = 0;
                    for (var i = 0; i < iovcnt; i++) {
                        var ptr = HEAP32[iov + i * 8 >> 2];
                        var len = HEAP32[iov + (i * 8 + 4) >> 2];
                        var curr = FS.write(stream, HEAP8, ptr, len, offset);
                        if (curr < 0) return -1;
                        ret += curr
                    }
                    return ret
                }, varargs: undefined, get: function () {
                    SYSCALLS.varargs += 4;
                    var ret = HEAP32[SYSCALLS.varargs - 4 >> 2];
                    return ret
                }, getStr: function (ptr) {
                    var ret = UTF8ToString(ptr);
                    return ret
                }, getStreamFromFD: function (fd) {
                    var stream = FS.getStream(fd);
                    if (!stream) throw new FS.ErrnoError(8);
                    return stream
                }, get64: function (low, high) {return low}
            };

            function ___sys_access(path, amode) {
                try {
                    path = SYSCALLS.getStr(path);
                    return SYSCALLS.doAccess(path, amode)
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_chdir(path) {
                try {
                    path = SYSCALLS.getStr(path);
                    FS.chdir(path);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_chmod(path, mode) {
                try {
                    path = SYSCALLS.getStr(path);
                    FS.chmod(path, mode);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            var ERRNO_CODES = {
                EPERM: 63, ENOENT: 44, ESRCH: 71, EINTR: 27, EIO: 29, ENXIO: 60, E2BIG: 1, ENOEXEC: 45, EBADF: 8, ECHILD: 12, EAGAIN: 6, EWOULDBLOCK: 6, ENOMEM: 48, EACCES: 2, EFAULT: 21, ENOTBLK: 105, EBUSY: 10, EEXIST: 20, EXDEV: 75, ENODEV: 43, ENOTDIR: 54, EISDIR: 31, EINVAL: 28, ENFILE: 41, EMFILE: 33, ENOTTY: 59, ETXTBSY: 74, EFBIG: 22, ENOSPC: 51, ESPIPE: 70, EROFS: 69, EMLINK: 34, EPIPE: 64, EDOM: 18, ERANGE: 68, ENOMSG: 49, EIDRM: 24, ECHRNG: 106, EL2NSYNC: 156, EL3HLT: 107, EL3RST: 108, ELNRNG: 109, EUNATCH: 110, ENOCSI: 111, EL2HLT: 112, EDEADLK: 16, ENOLCK: 46, EBADE: 113, EBADR: 114, EXFULL: 115, ENOANO: 104, EBADRQC: 103, EBADSLT: 102, EDEADLOCK: 16, EBFONT: 101, ENOSTR: 100, ENODATA: 116, ETIME: 117, ENOSR: 118, ENONET: 119, ENOPKG: 120, EREMOTE: 121, ENOLINK: 47, EADV: 122, ESRMNT: 123, ECOMM: 124, EPROTO: 65, EMULTIHOP: 36, EDOTDOT: 125, EBADMSG: 9, ENOTUNIQ: 126, EBADFD: 127, EREMCHG: 128, ELIBACC: 129, ELIBBAD: 130, ELIBSCN: 131, ELIBMAX: 132, ELIBEXEC: 133, ENOSYS: 52, ENOTEMPTY: 55, ENAMETOOLONG: 37, ELOOP: 32, EOPNOTSUPP: 138, EPFNOSUPPORT: 139, ECONNRESET: 15, ENOBUFS: 42, EAFNOSUPPORT: 5, EPROTOTYPE: 67, ENOTSOCK: 57, ENOPROTOOPT: 50, ESHUTDOWN: 140, ECONNREFUSED: 14, EADDRINUSE: 3, ECONNABORTED: 13, ENETUNREACH: 40, ENETDOWN: 38, ETIMEDOUT: 73, EHOSTDOWN: 142, EHOSTUNREACH: 23, EINPROGRESS: 26, EALREADY: 7, EDESTADDRREQ: 17, EMSGSIZE: 35, EPROTONOSUPPORT: 66, ESOCKTNOSUPPORT: 137, EADDRNOTAVAIL: 4, ENETRESET: 39, EISCONN: 30, ENOTCONN: 53, ETOOMANYREFS: 141, EUSERS: 136, EDQUOT: 19, ESTALE: 72, ENOTSUP: 138, ENOMEDIUM: 148, EILSEQ: 25, EOVERFLOW: 61, ECANCELED: 11, ENOTRECOVERABLE: 56, EOWNERDEAD: 62, ESTRPIPE: 135
            };
            var SOCKFS = {
                mount: function (mount) {
                    Module["websocket"] = Module["websocket"] && "object" === typeof Module["websocket"] ? Module["websocket"] : {};
                    Module["websocket"]._callbacks = {};
                    Module["websocket"]["on"] = function (event, callback) {
                        if ("function" === typeof callback) {this._callbacks[event] = callback}
                        return this
                    };
                    Module["websocket"].emit = function (event, param) {if ("function" === typeof this._callbacks[event]) {this._callbacks[event].call(this, param)}};
                    return FS.createNode(null, "/", 16384 | 511, 0)
                }, createSocket: function (family, type, protocol) {
                    type &= ~526336;
                    var streaming = type == 1;
                    if (protocol) {assert(streaming == (protocol == 6))}
                    var sock = { family: family, type: type, protocol: protocol, server: null, error: null, peers: {}, pending: [], recv_queue: [], sock_ops: SOCKFS.websocket_sock_ops };
                    var name = SOCKFS.nextname();
                    var node = FS.createNode(SOCKFS.root, name, 49152, 0);
                    node.sock = sock;
                    var stream = FS.createStream({ path: name, node: node, flags: 2, seekable: false, stream_ops: SOCKFS.stream_ops });
                    sock.stream = stream;
                    return sock
                }, getSocket: function (fd) {
                    var stream = FS.getStream(fd);
                    if (!stream || !FS.isSocket(stream.node.mode)) {return null}
                    return stream.node.sock
                }, stream_ops: {
                    poll: function (stream) {
                        var sock = stream.node.sock;
                        return sock.sock_ops.poll(sock)
                    }, ioctl: function (stream, request, varargs) {
                        var sock = stream.node.sock;
                        return sock.sock_ops.ioctl(sock, request, varargs)
                    }, read: function (stream, buffer, offset, length, position) {
                        var sock = stream.node.sock;
                        var msg = sock.sock_ops.recvmsg(sock, length);
                        if (!msg) {return 0}
                        buffer.set(msg.buffer, offset);
                        return msg.buffer.length
                    }, write: function (stream, buffer, offset, length, position) {
                        var sock = stream.node.sock;
                        return sock.sock_ops.sendmsg(sock, buffer, offset, length)
                    }, close: function (stream) {
                        var sock = stream.node.sock;
                        sock.sock_ops.close(sock)
                    }
                }, nextname: function () {
                    if (!SOCKFS.nextname.current) {SOCKFS.nextname.current = 0}
                    return "socket[" + SOCKFS.nextname.current++ + "]"
                }, websocket_sock_ops: {
                    createPeer: function (sock, addr, port) {
                        var ws;
                        if (typeof addr === "object") {
                            ws = addr;
                            addr = null;
                            port = null
                        }
                        if (ws) {
                            if (ws._socket) {
                                addr = ws._socket.remoteAddress;
                                port = ws._socket.remotePort
                            } else {
                                var result = /ws[s]?:\/\/([^:]+):(\d+)/.exec(ws.url);
                                if (!result) {throw new Error("WebSocket URL must be in the format ws(s)://address:port")}
                                addr = result[1];
                                port = parseInt(result[2], 10)
                            }
                        } else {
                            try {
                                var runtimeConfig = Module["websocket"] && "object" === typeof Module["websocket"];
                                var url = "ws:#".replace("#", "//");
                                if (runtimeConfig) {if ("string" === typeof Module["websocket"]["url"]) {url = Module["websocket"]["url"]}}
                                if (url === "ws://" || url === "wss://") {
                                    var parts = addr.split("/");
                                    url = url + parts[0] + ":" + port + "/" + parts.slice(1).join("/")
                                }
                                var subProtocols = "binary";
                                if (runtimeConfig) {if ("string" === typeof Module["websocket"]["subprotocol"]) {subProtocols = Module["websocket"]["subprotocol"]}}
                                var opts = undefined;
                                if (subProtocols !== "null") {
                                    subProtocols = subProtocols.replace(/^ +| +$/g, "").split(/ *, */);
                                    opts = ENVIRONMENT_IS_NODE ? { "protocol": subProtocols.toString() } : subProtocols
                                }
                                if (runtimeConfig && null === Module["websocket"]["subprotocol"]) {
                                    subProtocols = "null";
                                    opts = undefined
                                }
                                var WebSocketConstructor;
                                {WebSocketConstructor = WebSocket}
                                ws = new WebSocketConstructor(url, opts);
                                ws.binaryType = "arraybuffer"
                            } catch (e) {throw new FS.ErrnoError(ERRNO_CODES.EHOSTUNREACH)}
                        }
                        var peer = { addr: addr, port: port, socket: ws, dgram_send_queue: [] };
                        SOCKFS.websocket_sock_ops.addPeer(sock, peer);
                        SOCKFS.websocket_sock_ops.handlePeerEvents(sock, peer);
                        if (sock.type === 2 && typeof sock.sport !== "undefined") {peer.dgram_send_queue.push(new Uint8Array([255, 255, 255, 255, "p".charCodeAt(0), "o".charCodeAt(0), "r".charCodeAt(0), "t".charCodeAt(0), (sock.sport & 65280) >> 8, sock.sport & 255]))}
                        return peer
                    }, getPeer: function (sock, addr, port) {return sock.peers[addr + ":" + port]}, addPeer: function (sock, peer) {sock.peers[peer.addr + ":" + peer.port] = peer}, removePeer: function (sock, peer) {delete sock.peers[peer.addr + ":" + peer.port]}, handlePeerEvents: function (sock, peer) {
                        var first = true;
                        var handleOpen = function () {
                            Module["websocket"].emit("open", sock.stream.fd);
                            try {
                                var queued = peer.dgram_send_queue.shift();
                                while (queued) {
                                    peer.socket.send(queued);
                                    queued = peer.dgram_send_queue.shift()
                                }
                            } catch (e) {peer.socket.close()}
                        };

                        function handleMessage(data) {
                            if (typeof data === "string") {
                                var encoder = new TextEncoder;
                                data = encoder.encode(data)
                            } else {
                                assert(data.byteLength !== undefined);
                                if (data.byteLength == 0) {return} else {data = new Uint8Array(data)}
                            }
                            var wasfirst = first;
                            first = false;
                            if (wasfirst && data.length === 10 && data[0] === 255 && data[1] === 255 && data[2] === 255 && data[3] === 255 && data[4] === "p".charCodeAt(0) && data[5] === "o".charCodeAt(0) && data[6] === "r".charCodeAt(0) && data[7] === "t".charCodeAt(0)) {
                                var newport = data[8] << 8 | data[9];
                                SOCKFS.websocket_sock_ops.removePeer(sock, peer);
                                peer.port = newport;
                                SOCKFS.websocket_sock_ops.addPeer(sock, peer);
                                return
                            }
                            sock.recv_queue.push({ addr: peer.addr, port: peer.port, data: data });
                            Module["websocket"].emit("message", sock.stream.fd)
                        }

                        if (ENVIRONMENT_IS_NODE) {
                            peer.socket.on("open", handleOpen);
                            peer.socket.on("message", function (data, flags) {
                                if (!flags.binary) {return}
                                handleMessage(new Uint8Array(data).buffer)
                            });
                            peer.socket.on("close", function () {Module["websocket"].emit("close", sock.stream.fd)});
                            peer.socket.on("error", function (error) {
                                sock.error = ERRNO_CODES.ECONNREFUSED;
                                Module["websocket"].emit("error", [sock.stream.fd, sock.error, "ECONNREFUSED: Connection refused"])
                            })
                        } else {
                            peer.socket.onopen = handleOpen;
                            peer.socket.onclose = function () {Module["websocket"].emit("close", sock.stream.fd)};
                            peer.socket.onmessage = function peer_socket_onmessage(event) {handleMessage(event.data)};
                            peer.socket.onerror = function (error) {
                                sock.error = ERRNO_CODES.ECONNREFUSED;
                                Module["websocket"].emit("error", [sock.stream.fd, sock.error, "ECONNREFUSED: Connection refused"])
                            }
                        }
                    }, poll: function (sock) {
                        if (sock.type === 1 && sock.server) {return sock.pending.length ? 64 | 1 : 0}
                        var mask = 0;
                        var dest = sock.type === 1 ? SOCKFS.websocket_sock_ops.getPeer(sock, sock.daddr, sock.dport) : null;
                        if (sock.recv_queue.length || !dest || dest && dest.socket.readyState === dest.socket.CLOSING || dest && dest.socket.readyState === dest.socket.CLOSED) {mask |= 64 | 1}
                        if (!dest || dest && dest.socket.readyState === dest.socket.OPEN) {mask |= 4}
                        if (dest && dest.socket.readyState === dest.socket.CLOSING || dest && dest.socket.readyState === dest.socket.CLOSED) {mask |= 16}
                        return mask
                    }, ioctl: function (sock, request, arg) {
                        switch (request) {
                            case 21531:
                                var bytes = 0;
                                if (sock.recv_queue.length) {bytes = sock.recv_queue[0].data.length}
                                HEAP32[arg >> 2] = bytes;
                                return 0;
                            default:
                                return ERRNO_CODES.EINVAL
                        }
                    }, close: function (sock) {
                        if (sock.server) {
                            try {sock.server.close()} catch (e) {}
                            sock.server = null
                        }
                        var peers = Object.keys(sock.peers);
                        for (var i = 0; i < peers.length; i++) {
                            var peer = sock.peers[peers[i]];
                            try {peer.socket.close()} catch (e) {}
                            SOCKFS.websocket_sock_ops.removePeer(sock, peer)
                        }
                        return 0
                    }, bind: function (sock, addr, port) {
                        if (typeof sock.saddr !== "undefined" || typeof sock.sport !== "undefined") {throw new FS.ErrnoError(ERRNO_CODES.EINVAL)}
                        sock.saddr = addr;
                        sock.sport = port;
                        if (sock.type === 2) {
                            if (sock.server) {
                                sock.server.close();
                                sock.server = null
                            }
                            try {sock.sock_ops.listen(sock, 0)} catch (e) {
                                if (!(e instanceof FS.ErrnoError)) throw e;
                                if (e.errno !== ERRNO_CODES.EOPNOTSUPP) throw e
                            }
                        }
                    }, connect: function (sock, addr, port) {
                        if (sock.server) {throw new FS.ErrnoError(ERRNO_CODES.EOPNOTSUPP)}
                        if (typeof sock.daddr !== "undefined" && typeof sock.dport !== "undefined") {
                            var dest = SOCKFS.websocket_sock_ops.getPeer(sock, sock.daddr, sock.dport);
                            if (dest) {if (dest.socket.readyState === dest.socket.CONNECTING) {throw new FS.ErrnoError(ERRNO_CODES.EALREADY)} else {throw new FS.ErrnoError(ERRNO_CODES.EISCONN)}}
                        }
                        var peer = SOCKFS.websocket_sock_ops.createPeer(sock, addr, port);
                        sock.daddr = peer.addr;
                        sock.dport = peer.port;
                        throw new FS.ErrnoError(ERRNO_CODES.EINPROGRESS)
                    }, listen: function (sock, backlog) {if (!ENVIRONMENT_IS_NODE) {throw new FS.ErrnoError(ERRNO_CODES.EOPNOTSUPP)}}, accept: function (listensock) {
                        if (!listensock.server) {throw new FS.ErrnoError(ERRNO_CODES.EINVAL)}
                        var newsock = listensock.pending.shift();
                        newsock.stream.flags = listensock.stream.flags;
                        return newsock
                    }, getname: function (sock, peer) {
                        var addr,
                            port;
                        if (peer) {
                            if (sock.daddr === undefined || sock.dport === undefined) {throw new FS.ErrnoError(ERRNO_CODES.ENOTCONN)}
                            addr = sock.daddr;
                            port = sock.dport
                        } else {
                            addr = sock.saddr || 0;
                            port = sock.sport || 0
                        }
                        return { addr: addr, port: port }
                    }, sendmsg: function (sock, buffer, offset, length, addr, port) {
                        if (sock.type === 2) {
                            if (addr === undefined || port === undefined) {
                                addr = sock.daddr;
                                port = sock.dport
                            }
                            if (addr === undefined || port === undefined) {throw new FS.ErrnoError(ERRNO_CODES.EDESTADDRREQ)}
                        } else {
                            addr = sock.daddr;
                            port = sock.dport
                        }
                        var dest = SOCKFS.websocket_sock_ops.getPeer(sock, addr, port);
                        if (sock.type === 1) {if (!dest || dest.socket.readyState === dest.socket.CLOSING || dest.socket.readyState === dest.socket.CLOSED) {throw new FS.ErrnoError(ERRNO_CODES.ENOTCONN)} else if (dest.socket.readyState === dest.socket.CONNECTING) {throw new FS.ErrnoError(ERRNO_CODES.EAGAIN)}}
                        if (ArrayBuffer.isView(buffer)) {
                            offset += buffer.byteOffset;
                            buffer = buffer.buffer
                        }
                        var data;
                        data = buffer.slice(offset, offset + length);
                        if (sock.type === 2) {
                            if (!dest || dest.socket.readyState !== dest.socket.OPEN) {
                                if (!dest || dest.socket.readyState === dest.socket.CLOSING || dest.socket.readyState === dest.socket.CLOSED) {dest = SOCKFS.websocket_sock_ops.createPeer(sock, addr, port)}
                                dest.dgram_send_queue.push(data);
                                return length
                            }
                        }
                        try {
                            dest.socket.send(data);
                            return length
                        } catch (e) {throw new FS.ErrnoError(ERRNO_CODES.EINVAL)}
                    }, recvmsg: function (sock, length) {
                        if (sock.type === 1 && sock.server) {throw new FS.ErrnoError(ERRNO_CODES.ENOTCONN)}
                        var queued = sock.recv_queue.shift();
                        if (!queued) {
                            if (sock.type === 1) {
                                var dest = SOCKFS.websocket_sock_ops.getPeer(sock, sock.daddr, sock.dport);
                                if (!dest) {throw new FS.ErrnoError(ERRNO_CODES.ENOTCONN)} else if (dest.socket.readyState === dest.socket.CLOSING || dest.socket.readyState === dest.socket.CLOSED) {return null} else {throw new FS.ErrnoError(ERRNO_CODES.EAGAIN)}
                            } else {throw new FS.ErrnoError(ERRNO_CODES.EAGAIN)}
                        }
                        var queuedLength = queued.data.byteLength || queued.data.length;
                        var queuedOffset = queued.data.byteOffset || 0;
                        var queuedBuffer = queued.data.buffer || queued.data;
                        var bytesRead = Math.min(length, queuedLength);
                        var res = { buffer: new Uint8Array(queuedBuffer, queuedOffset, bytesRead), addr: queued.addr, port: queued.port };
                        if (sock.type === 1 && bytesRead < queuedLength) {
                            var bytesRemaining = queuedLength - bytesRead;
                            queued.data = new Uint8Array(queuedBuffer, queuedOffset + bytesRead, bytesRemaining);
                            sock.recv_queue.unshift(queued)
                        }
                        return res
                    }
                }
            };

            function getSocketFromFD(fd) {
                var socket = SOCKFS.getSocket(fd);
                if (!socket) throw new FS.ErrnoError(8);
                return socket
            }

            function inetNtop4(addr) {return (addr & 255) + "." + (addr >> 8 & 255) + "." + (addr >> 16 & 255) + "." + (addr >> 24 & 255)}

            function inetNtop6(ints) {
                var str = "";
                var word = 0;
                var longest = 0;
                var lastzero = 0;
                var zstart = 0;
                var len = 0;
                var i = 0;
                var parts = [ints[0] & 65535, ints[0] >> 16, ints[1] & 65535, ints[1] >> 16, ints[2] & 65535, ints[2] >> 16, ints[3] & 65535, ints[3] >> 16];
                var hasipv4 = true;
                var v4part = "";
                for (i = 0; i < 5; i++) {
                    if (parts[i] !== 0) {
                        hasipv4 = false;
                        break
                    }
                }
                if (hasipv4) {
                    v4part = inetNtop4(parts[6] | parts[7] << 16);
                    if (parts[5] === -1) {
                        str = "::ffff:";
                        str += v4part;
                        return str
                    }
                    if (parts[5] === 0) {
                        str = "::";
                        if (v4part === "0.0.0.0") v4part = "";
                        if (v4part === "0.0.0.1") v4part = "1";
                        str += v4part;
                        return str
                    }
                }
                for (word = 0; word < 8; word++) {
                    if (parts[word] === 0) {
                        if (word - lastzero > 1) {len = 0}
                        lastzero = word;
                        len++
                    }
                    if (len > longest) {
                        longest = len;
                        zstart = word - longest + 1
                    }
                }
                for (word = 0; word < 8; word++) {
                    if (longest > 1) {
                        if (parts[word] === 0 && word >= zstart && word < zstart + longest) {
                            if (word === zstart) {
                                str += ":";
                                if (zstart === 0) str += ":"
                            }
                            continue
                        }
                    }
                    str += Number(_ntohs(parts[word] & 65535)).toString(16);
                    str += word < 7 ? ":" : ""
                }
                return str
            }

            function readSockaddr(sa, salen) {
                var family = HEAP16[sa >> 1];
                var port = _ntohs(HEAPU16[sa + 2 >> 1]);
                var addr;
                switch (family) {
                    case 2:
                        if (salen !== 16) {return { errno: 28 }}
                        addr = HEAP32[sa + 4 >> 2];
                        addr = inetNtop4(addr);
                        break;
                    case 10:
                        if (salen !== 28) {return { errno: 28 }}
                        addr = [HEAP32[sa + 8 >> 2], HEAP32[sa + 12 >> 2], HEAP32[sa + 16 >> 2], HEAP32[sa + 20 >> 2]];
                        addr = inetNtop6(addr);
                        break;
                    default:
                        return { errno: 5 }
                }
                return { family: family, addr: addr, port: port }
            }

            function getSocketAddress(addrp, addrlen, allowNull) {
                if (allowNull && addrp === 0) return null;
                var info = readSockaddr(addrp, addrlen);
                if (info.errno) throw new FS.ErrnoError(info.errno);
                info.addr = DNS.lookup_addr(info.addr) || info.addr;
                return info
            }

            function ___sys_connect(fd, addr, addrlen) {
                try {
                    var sock = getSocketFromFD(fd);
                    var info = getSocketAddress(addr, addrlen);
                    sock.sock_ops.connect(sock, info.addr, info.port);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_fadvise64_64(fd, offset, len, advice) {return 0}

            function ___sys_fchmod(fd, mode) {
                try {
                    FS.fchmod(fd, mode);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_fcntl64(fd, cmd, varargs) {
                SYSCALLS.varargs = varargs;
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    switch (cmd) {
                        case 0: {
                            var arg = SYSCALLS.get();
                            if (arg < 0) {return -28}
                            var newStream;
                            newStream = FS.open(stream.path, stream.flags, 0, arg);
                            return newStream.fd
                        }
                        case 1:
                        case 2:
                            return 0;
                        case 3:
                            return stream.flags;
                        case 4: {
                            var arg = SYSCALLS.get();
                            stream.flags |= arg;
                            return 0
                        }
                        case 12: {
                            var arg = SYSCALLS.get();
                            var offset = 0;
                            HEAP16[arg + offset >> 1] = 2;
                            return 0
                        }
                        case 13:
                        case 14:
                            return 0;
                        case 16:
                        case 8:
                            return -28;
                        case 9:
                            setErrNo(28);
                            return -1;
                        default: {return -28}
                    }
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_fstat64(fd, buf) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    return SYSCALLS.doStat(FS.stat, stream.path, buf)
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_fstatfs64(fd, size, buf) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    return ___sys_statfs64(0, size, buf)
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_ftruncate64(fd, zero, low, high) {
                try {
                    var length = SYSCALLS.get64(low, high);
                    FS.ftruncate(fd, length);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_getcwd(buf, size) {
                try {
                    if (size === 0) return -28;
                    var cwd = FS.cwd();
                    var cwdLengthInBytes = lengthBytesUTF8(cwd);
                    if (size < cwdLengthInBytes + 1) return -68;
                    stringToUTF8(cwd, buf, size);
                    return buf
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_getdents64(fd, dirp, count) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    if (!stream.getdents) {stream.getdents = FS.readdir(stream.path)}
                    var struct_size = 280;
                    var pos = 0;
                    var off = FS.llseek(stream, 0, 1);
                    var idx = Math.floor(off / struct_size);
                    while (idx < stream.getdents.length && pos + struct_size <= count) {
                        var id;
                        var type;
                        var name = stream.getdents[idx];
                        if (name[0] === ".") {
                            id = 1;
                            type = 4
                        } else {
                            var child = FS.lookupNode(stream.node, name);
                            id = child.id;
                            type = FS.isChrdev(child.mode) ? 2 : FS.isDir(child.mode) ? 4 : FS.isLink(child.mode) ? 10 : 8
                        }
                        tempI64 = [id >>> 0, (tempDouble = id, +Math.abs(tempDouble) >= 1 ? tempDouble > 0 ? (Math.min(+Math.floor(tempDouble / 4294967296), 4294967295) | 0) >>> 0 : ~~+Math.ceil((tempDouble - +(~~tempDouble >>> 0)) / 4294967296) >>> 0 : 0)], HEAP32[dirp + pos >> 2] = tempI64[0], HEAP32[dirp + pos + 4 >> 2] = tempI64[1];
                        tempI64 = [(idx + 1) * struct_size >>> 0, (tempDouble = (idx + 1) * struct_size, +Math.abs(tempDouble) >= 1 ? tempDouble > 0 ? (Math.min(+Math.floor(tempDouble / 4294967296), 4294967295) | 0) >>> 0 : ~~+Math.ceil((tempDouble - +(~~tempDouble >>> 0)) / 4294967296) >>> 0 : 0)], HEAP32[dirp + pos + 8 >> 2] = tempI64[0], HEAP32[dirp + pos + 12 >> 2] = tempI64[1];
                        HEAP16[dirp + pos + 16 >> 1] = 280;
                        HEAP8[dirp + pos + 18 >> 0] = type;
                        stringToUTF8(name, dirp + pos + 19, 256);
                        pos += struct_size;
                        idx += 1
                    }
                    FS.llseek(stream, idx * struct_size, 0);
                    return pos
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_getpid() {return 42}

            function ___sys_getrusage(who, usage) {
                try {
                    _memset(usage, 0, 136);
                    HEAP32[usage >> 2] = 1;
                    HEAP32[usage + 4 >> 2] = 2;
                    HEAP32[usage + 8 >> 2] = 3;
                    HEAP32[usage + 12 >> 2] = 4;
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_ioctl(fd, op, varargs) {
                SYSCALLS.varargs = varargs;
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    switch (op) {
                        case 21509:
                        case 21505: {
                            if (!stream.tty) return -59;
                            return 0
                        }
                        case 21510:
                        case 21511:
                        case 21512:
                        case 21506:
                        case 21507:
                        case 21508: {
                            if (!stream.tty) return -59;
                            return 0
                        }
                        case 21519: {
                            if (!stream.tty) return -59;
                            var argp = SYSCALLS.get();
                            HEAP32[argp >> 2] = 0;
                            return 0
                        }
                        case 21520: {
                            if (!stream.tty) return -59;
                            return -28
                        }
                        case 21531: {
                            var argp = SYSCALLS.get();
                            return FS.ioctl(stream, op, argp)
                        }
                        case 21523: {
                            if (!stream.tty) return -59;
                            return 0
                        }
                        case 21524: {
                            if (!stream.tty) return -59;
                            return 0
                        }
                        default:
                            abort("bad ioctl syscall " + op)
                    }
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_link(oldpath, newpath) {return -34}

            function ___sys_lstat64(path, buf) {
                try {
                    path = SYSCALLS.getStr(path);
                    return SYSCALLS.doStat(FS.lstat, path, buf)
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_madvise1(addr, length, advice) {return 0}

            function ___sys_mkdir(path, mode) {
                try {
                    path = SYSCALLS.getStr(path);
                    return SYSCALLS.doMkdir(path, mode)
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function syscallMmap2(addr, len, prot, flags, fd, off) {
                off <<= 12;
                var ptr;
                var allocated = false;
                if ((flags & 16) !== 0 && addr % 65536 !== 0) {return -28}
                if ((flags & 32) !== 0) {
                    ptr = _memalign(65536, len);
                    if (!ptr) return -48;
                    _memset(ptr, 0, len);
                    allocated = true
                } else {
                    var info = FS.getStream(fd);
                    if (!info) return -8;
                    var res = FS.mmap(info, addr, len, off, prot, flags);
                    ptr = res.ptr;
                    allocated = res.allocated
                }
                SYSCALLS.mappings[ptr] = { malloc: ptr, len: len, allocated: allocated, fd: fd, prot: prot, flags: flags, offset: off };
                return ptr
            }

            function ___sys_mmap2(addr, len, prot, flags, fd, off) {
                try {return syscallMmap2(addr, len, prot, flags, fd, off)} catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_msync(addr, len, flags) {
                try {
                    var info = SYSCALLS.mappings[addr];
                    if (!info) return 0;
                    SYSCALLS.doMsync(addr, FS.getStream(info.fd), len, info.flags, 0);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function syscallMunmap(addr, len) {
                if ((addr | 0) === -1 || len === 0) {return -28}
                var info = SYSCALLS.mappings[addr];
                if (!info) return 0;
                if (len === info.len) {
                    var stream = FS.getStream(info.fd);
                    if (stream) {
                        if (info.prot & 2) {SYSCALLS.doMsync(addr, stream, len, info.flags, info.offset)}
                        FS.munmap(stream)
                    }
                    SYSCALLS.mappings[addr] = null;
                    if (info.allocated) {_free(info.malloc)}
                }
                return 0
            }

            function ___sys_munmap(addr, len) {
                try {return syscallMunmap(addr, len)} catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_open(path, flags, varargs) {
                SYSCALLS.varargs = varargs;
                try {
                    var pathname = SYSCALLS.getStr(path);
                    var mode = varargs ? SYSCALLS.get() : 0;
                    var stream = FS.open(pathname, flags, mode);
                    return stream.fd
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_readlink(path, buf, bufsize) {
                try {
                    path = SYSCALLS.getStr(path);
                    return SYSCALLS.doReadlink(path, buf, bufsize)
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function inetPton4(str) {
                var b = str.split(".");
                for (var i = 0; i < 4; i++) {
                    var tmp = Number(b[i]);
                    if (isNaN(tmp)) return null;
                    b[i] = tmp
                }
                return (b[0] | b[1] << 8 | b[2] << 16 | b[3] << 24) >>> 0
            }

            function jstoi_q(str) {return parseInt(str)}

            function inetPton6(str) {
                var words;
                var w,
                    offset,
                    z;
                var valid6regx = /^((?=.*::)(?!.*::.+::)(::)?([\dA-F]{1,4}:(:|\b)|){5}|([\dA-F]{1,4}:){6})((([\dA-F]{1,4}((?!\3)::|:\b|$))|(?!\2\3)){2}|(((2[0-4]|1\d|[1-9])?\d|25[0-5])\.?\b){4})$/i;
                var parts = [];
                if (!valid6regx.test(str)) {return null}
                if (str === "::") {return [0, 0, 0, 0, 0, 0, 0, 0]}
                if (str.startsWith("::")) {str = str.replace("::", "Z:")} else {str = str.replace("::", ":Z:")}
                if (str.indexOf(".") > 0) {
                    str = str.replace(new RegExp("[.]", "g"), ":");
                    words = str.split(":");
                    words[words.length - 4] = jstoi_q(words[words.length - 4]) + jstoi_q(words[words.length - 3]) * 256;
                    words[words.length - 3] = jstoi_q(words[words.length - 2]) + jstoi_q(words[words.length - 1]) * 256;
                    words = words.slice(0, words.length - 2)
                } else {words = str.split(":")}
                offset = 0;
                z = 0;
                for (w = 0; w < words.length; w++) {
                    if (typeof words[w] === "string") {
                        if (words[w] === "Z") {
                            for (z = 0; z < 8 - words.length + 1; z++) {parts[w + z] = 0}
                            offset = z - 1
                        } else {parts[w + offset] = _htons(parseInt(words[w], 16))}
                    } else {parts[w + offset] = words[w]}
                }
                return [parts[1] << 16 | parts[0], parts[3] << 16 | parts[2], parts[5] << 16 | parts[4], parts[7] << 16 | parts[6]]
            }

            function writeSockaddr(sa, family, addr, port, addrlen) {
                switch (family) {
                    case 2:
                        addr = inetPton4(addr);
                        if (addrlen) {HEAP32[addrlen >> 2] = 16}
                        HEAP16[sa >> 1] = family;
                        HEAP32[sa + 4 >> 2] = addr;
                        HEAP16[sa + 2 >> 1] = _htons(port);
                        tempI64 = [0 >>> 0, (tempDouble = 0, +Math.abs(tempDouble) >= 1 ? tempDouble > 0 ? (Math.min(+Math.floor(tempDouble / 4294967296), 4294967295) | 0) >>> 0 : ~~+Math.ceil((tempDouble - +(~~tempDouble >>> 0)) / 4294967296) >>> 0 : 0)], HEAP32[sa + 8 >> 2] = tempI64[0], HEAP32[sa + 12 >> 2] = tempI64[1];
                        break;
                    case 10:
                        addr = inetPton6(addr);
                        if (addrlen) {HEAP32[addrlen >> 2] = 28}
                        HEAP32[sa >> 2] = family;
                        HEAP32[sa + 8 >> 2] = addr[0];
                        HEAP32[sa + 12 >> 2] = addr[1];
                        HEAP32[sa + 16 >> 2] = addr[2];
                        HEAP32[sa + 20 >> 2] = addr[3];
                        HEAP16[sa + 2 >> 1] = _htons(port);
                        HEAP32[sa + 4 >> 2] = 0;
                        HEAP32[sa + 24 >> 2] = 0;
                        break;
                    default:
                        return 5
                }
                return 0
            }

            var DNS = {
                address_map: { id: 1, addrs: {}, names: {} }, lookup_name: function (name) {
                    var res = inetPton4(name);
                    if (res !== null) {return name}
                    res = inetPton6(name);
                    if (res !== null) {return name}
                    var addr;
                    if (DNS.address_map.addrs[name]) {addr = DNS.address_map.addrs[name]} else {
                        var id = DNS.address_map.id++;
                        assert(id < 65535, "exceeded max address mappings of 65535");
                        addr = "172.29." + (id & 255) + "." + (id & 65280);
                        DNS.address_map.names[addr] = name;
                        DNS.address_map.addrs[name] = addr
                    }
                    return addr
                }, lookup_addr: function (addr) {
                    if (DNS.address_map.names[addr]) {return DNS.address_map.names[addr]}
                    return null
                }
            };

            function ___sys_recvfrom(fd, buf, len, flags, addr, addrlen) {
                try {
                    var sock = getSocketFromFD(fd);
                    var msg = sock.sock_ops.recvmsg(sock, len);
                    if (!msg) return 0;
                    if (addr) {var errno = writeSockaddr(addr, sock.family, DNS.lookup_name(msg.addr), msg.port, addrlen)}
                    HEAPU8.set(msg.buffer, buf);
                    return msg.buffer.byteLength
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_rename(old_path, new_path) {
                try {
                    old_path = SYSCALLS.getStr(old_path);
                    new_path = SYSCALLS.getStr(new_path);
                    FS.rename(old_path, new_path);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_rmdir(path) {
                try {
                    path = SYSCALLS.getStr(path);
                    FS.rmdir(path);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_sendto(fd, message, length, flags, addr, addr_len) {
                try {
                    var sock = getSocketFromFD(fd);
                    var dest = getSocketAddress(addr, addr_len, true);
                    if (!dest) {return FS.write(sock.stream, HEAP8, message, length)} else {return sock.sock_ops.sendmsg(sock, HEAP8, message, length, dest.addr, dest.port)}
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_setsockopt(fd) {
                try {return -50} catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_shutdown(fd, how) {
                try {
                    getSocketFromFD(fd);
                    return -52
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_socket(domain, type, protocol) {
                try {
                    var sock = SOCKFS.createSocket(domain, type, protocol);
                    return sock.stream.fd
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_stat64(path, buf) {
                try {
                    path = SYSCALLS.getStr(path);
                    return SYSCALLS.doStat(FS.stat, path, buf)
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_symlink(target, linkpath) {
                try {
                    target = SYSCALLS.getStr(target);
                    linkpath = SYSCALLS.getStr(linkpath);
                    FS.symlink(target, linkpath);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_unlink(path) {
                try {
                    path = SYSCALLS.getStr(path);
                    FS.unlink(path);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function ___sys_utimensat(dirfd, path, times, flags) {
                try {
                    path = SYSCALLS.getStr(path);
                    path = SYSCALLS.calculateAt(dirfd, path, true);
                    var seconds = HEAP32[times >> 2];
                    var nanoseconds = HEAP32[times + 4 >> 2];
                    var atime = seconds * 1e3 + nanoseconds / (1e3 * 1e3);
                    times += 8;
                    seconds = HEAP32[times >> 2];
                    nanoseconds = HEAP32[times + 4 >> 2];
                    var mtime = seconds * 1e3 + nanoseconds / (1e3 * 1e3);
                    FS.utime(path, atime, mtime);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return -e.errno
                }
            }

            function _abort() {abort()}

            function _emscripten_get_now_res() {return 1e3}

            function _clock_getres(clk_id, res) {
                var nsec;
                if (clk_id === 0) {nsec = 1e3 * 1e3} else if (clk_id === 1 && _emscripten_get_now_is_monotonic) {nsec = _emscripten_get_now_res()} else {
                    setErrNo(28);
                    return -1
                }
                HEAP32[res >> 2] = nsec / 1e9 | 0;
                HEAP32[res + 4 >> 2] = nsec;
                return 0
            }

            function _difftime(time1, time0) {return time1 - time0}

            var DOTNETENTROPY = {
                batchedQuotaMax: 65536, getBatchedRandomValues: function (buffer, bufferLength) {
                    for (var i = 0; i < bufferLength; i += this.batchedQuotaMax) {
                        var view = new Uint8Array(Module.HEAPU8.buffer, buffer + i, Math.min(bufferLength - i, this.batchedQuotaMax));
                        crypto.getRandomValues(view)
                    }
                }
            };

            function _dotnet_browser_entropy(buffer, bufferLength) {
                if (typeof crypto === "object" && typeof crypto["getRandomValues"] === "function") {
                    DOTNETENTROPY.getBatchedRandomValues(buffer, bufferLength);
                    return 0
                } else {return -1}
            }

            var readAsmConstArgsArray = [];

            function readAsmConstArgs(sigPtr, buf) {
                readAsmConstArgsArray.length = 0;
                var ch;
                buf >>= 2;
                while (ch = HEAPU8[sigPtr++]) {
                    var double = ch < 105;
                    if (double && buf & 1) buf++;
                    readAsmConstArgsArray.push(double ? HEAPF64[buf++ >> 1] : HEAP32[buf]);
                    ++buf
                }
                return readAsmConstArgsArray
            }

            function _emscripten_asm_const_int(code, sigPtr, argbuf) {
                var args = readAsmConstArgs(sigPtr, argbuf);
                return ASM_CONSTS[code].apply(null, args)
            }

            function _emscripten_get_heap_max() {return 2147483648}

            function _emscripten_memcpy_big(dest, src, num) {HEAPU8.copyWithin(dest, src, src + num)}

            function emscripten_realloc_buffer(size) {
                try {
                    wasmMemory.grow(size - buffer.byteLength + 65535 >>> 16);
                    updateGlobalBufferAndViews(wasmMemory.buffer);
                    return 1
                } catch (e) {}
            }

            function _emscripten_resize_heap(requestedSize) {
                var oldSize = HEAPU8.length;
                requestedSize = requestedSize >>> 0;
                var maxHeapSize = 2147483648;
                if (requestedSize > maxHeapSize) {return false}
                for (var cutDown = 1; cutDown <= 4; cutDown *= 2) {
                    var overGrownHeapSize = oldSize * (1 + .2 / cutDown);
                    overGrownHeapSize = Math.min(overGrownHeapSize, requestedSize + 100663296);
                    var newSize = Math.min(maxHeapSize, alignUp(Math.max(requestedSize, overGrownHeapSize), 65536));
                    var replacement = emscripten_realloc_buffer(newSize);
                    if (replacement) {return true}
                }
                return false
            }

            function _emscripten_thread_sleep(msecs) {
                var start = _emscripten_get_now();
                while (_emscripten_get_now() - start < msecs) {}
            }

            var ENV = {};

            function getExecutableName() {return thisProgram || "./this.program"}

            function getEnvStrings() {
                if (!getEnvStrings.strings) {
                    var lang = (typeof navigator === "object" && navigator.languages && navigator.languages[0] || "C").replace("-", "_") + ".UTF-8";
                    var env = { "USER": "web_user", "LOGNAME": "web_user", "PATH": "/", "PWD": "/", "HOME": "/home/web_user", "LANG": lang, "_": getExecutableName() };
                    for (var x in ENV) {env[x] = ENV[x]}
                    var strings = [];
                    for (var x in env) {strings.push(x + "=" + env[x])}
                    getEnvStrings.strings = strings
                }
                return getEnvStrings.strings
            }

            function _environ_get(__environ, environ_buf) {
                try {
                    var bufSize = 0;
                    getEnvStrings().forEach(function (string, i) {
                        var ptr = environ_buf + bufSize;
                        HEAP32[__environ + i * 4 >> 2] = ptr;
                        writeAsciiToMemory(string, ptr);
                        bufSize += string.length + 1
                    });
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _environ_sizes_get(penviron_count, penviron_buf_size) {
                try {
                    var strings = getEnvStrings();
                    HEAP32[penviron_count >> 2] = strings.length;
                    var bufSize = 0;
                    strings.forEach(function (string) {bufSize += string.length + 1});
                    HEAP32[penviron_buf_size >> 2] = bufSize;
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _exit(status) {exit(status)}

            function _fd_close(fd) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    FS.close(stream);
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _fd_fdstat_get(fd, pbuf) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    var type = stream.tty ? 2 : FS.isDir(stream.mode) ? 3 : FS.isLink(stream.mode) ? 7 : 4;
                    HEAP8[pbuf >> 0] = type;
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _fd_pread(fd, iov, iovcnt, offset_low, offset_high, pnum) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    var num = SYSCALLS.doReadv(stream, iov, iovcnt, offset_low);
                    HEAP32[pnum >> 2] = num;
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _fd_pwrite(fd, iov, iovcnt, offset_low, offset_high, pnum) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    var num = SYSCALLS.doWritev(stream, iov, iovcnt, offset_low);
                    HEAP32[pnum >> 2] = num;
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _fd_read(fd, iov, iovcnt, pnum) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    var num = SYSCALLS.doReadv(stream, iov, iovcnt);
                    HEAP32[pnum >> 2] = num;
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _fd_seek(fd, offset_low, offset_high, whence, newOffset) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    var HIGH_OFFSET = 4294967296;
                    var offset = offset_high * HIGH_OFFSET + (offset_low >>> 0);
                    var DOUBLE_LIMIT = 9007199254740992;
                    if (offset <= -DOUBLE_LIMIT || offset >= DOUBLE_LIMIT) {return -61}
                    FS.llseek(stream, offset, whence);
                    tempI64 = [stream.position >>> 0, (tempDouble = stream.position, +Math.abs(tempDouble) >= 1 ? tempDouble > 0 ? (Math.min(+Math.floor(tempDouble / 4294967296), 4294967295) | 0) >>> 0 : ~~+Math.ceil((tempDouble - +(~~tempDouble >>> 0)) / 4294967296) >>> 0 : 0)], HEAP32[newOffset >> 2] = tempI64[0], HEAP32[newOffset + 4 >> 2] = tempI64[1];
                    if (stream.getdents && offset === 0 && whence === 0) stream.getdents = null;
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _fd_sync(fd) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    if (stream.stream_ops && stream.stream_ops.fsync) {return -stream.stream_ops.fsync(stream)}
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _fd_write(fd, iov, iovcnt, pnum) {
                try {
                    var stream = SYSCALLS.getStreamFromFD(fd);
                    var num = SYSCALLS.doWritev(stream, iov, iovcnt);
                    HEAP32[pnum >> 2] = num;
                    return 0
                } catch (e) {
                    if (typeof FS === "undefined" || !(e instanceof FS.ErrnoError)) abort(e);
                    return e.errno
                }
            }

            function _flock(fd, operation) {return 0}

            var GAI_ERRNO_MESSAGES = {};

            function _gai_strerror(val) {
                var buflen = 256;
                if (!_gai_strerror.buffer) {
                    _gai_strerror.buffer = _malloc(buflen);
                    GAI_ERRNO_MESSAGES["0"] = "Success";
                    GAI_ERRNO_MESSAGES["" + -1] = "Invalid value for 'ai_flags' field";
                    GAI_ERRNO_MESSAGES["" + -2] = "NAME or SERVICE is unknown";
                    GAI_ERRNO_MESSAGES["" + -3] = "Temporary failure in name resolution";
                    GAI_ERRNO_MESSAGES["" + -4] = "Non-recoverable failure in name res";
                    GAI_ERRNO_MESSAGES["" + -6] = "'ai_family' not supported";
                    GAI_ERRNO_MESSAGES["" + -7] = "'ai_socktype' not supported";
                    GAI_ERRNO_MESSAGES["" + -8] = "SERVICE not supported for 'ai_socktype'";
                    GAI_ERRNO_MESSAGES["" + -10] = "Memory allocation failure";
                    GAI_ERRNO_MESSAGES["" + -11] = "System error returned in 'errno'";
                    GAI_ERRNO_MESSAGES["" + -12] = "Argument buffer overflow"
                }
                var msg = "Unknown error";
                if (val in GAI_ERRNO_MESSAGES) {if (GAI_ERRNO_MESSAGES[val].length > buflen - 1) {msg = "Message too long"} else {msg = GAI_ERRNO_MESSAGES[val]}}
                writeAsciiToMemory(msg, _gai_strerror.buffer);
                return _gai_strerror.buffer
            }

            function _getTempRet0() {return getTempRet0()}

            function _gettimeofday(ptr) {
                var now = Date.now();
                HEAP32[ptr >> 2] = now / 1e3 | 0;
                HEAP32[ptr + 4 >> 2] = now % 1e3 * 1e3 | 0;
                return 0
            }

            function _gmtime_r(time, tmPtr) {
                var date = new Date(HEAP32[time >> 2] * 1e3);
                HEAP32[tmPtr >> 2] = date.getUTCSeconds();
                HEAP32[tmPtr + 4 >> 2] = date.getUTCMinutes();
                HEAP32[tmPtr + 8 >> 2] = date.getUTCHours();
                HEAP32[tmPtr + 12 >> 2] = date.getUTCDate();
                HEAP32[tmPtr + 16 >> 2] = date.getUTCMonth();
                HEAP32[tmPtr + 20 >> 2] = date.getUTCFullYear() - 1900;
                HEAP32[tmPtr + 24 >> 2] = date.getUTCDay();
                HEAP32[tmPtr + 36 >> 2] = 0;
                HEAP32[tmPtr + 32 >> 2] = 0;
                var start = Date.UTC(date.getUTCFullYear(), 0, 1, 0, 0, 0, 0);
                var yday = (date.getTime() - start) / (1e3 * 60 * 60 * 24) | 0;
                HEAP32[tmPtr + 28 >> 2] = yday;
                if (!_gmtime_r.GMTString) _gmtime_r.GMTString = allocateUTF8("GMT");
                HEAP32[tmPtr + 40 >> 2] = _gmtime_r.GMTString;
                return tmPtr
            }

            function _llvm_eh_typeid_for(type) {return type}

            function _tzset() {
                if (_tzset.called) return;
                _tzset.called = true;
                var currentYear = (new Date).getFullYear();
                var winter = new Date(currentYear, 0, 1);
                var summer = new Date(currentYear, 6, 1);
                var winterOffset = winter.getTimezoneOffset();
                var summerOffset = summer.getTimezoneOffset();
                var stdTimezoneOffset = Math.max(winterOffset, summerOffset);
                HEAP32[__get_timezone() >> 2] = stdTimezoneOffset * 60;
                HEAP32[__get_daylight() >> 2] = Number(winterOffset != summerOffset);

                function extractZone(date) {
                    var match = date.toTimeString().match(/\(([A-Za-z ]+)\)$/);
                    return match ? match[1] : "GMT"
                }

                var winterName = extractZone(winter);
                var summerName = extractZone(summer);
                var winterNamePtr = allocateUTF8(winterName);
                var summerNamePtr = allocateUTF8(summerName);
                if (summerOffset < winterOffset) {
                    HEAP32[__get_tzname() >> 2] = winterNamePtr;
                    HEAP32[__get_tzname() + 4 >> 2] = summerNamePtr
                } else {
                    HEAP32[__get_tzname() >> 2] = summerNamePtr;
                    HEAP32[__get_tzname() + 4 >> 2] = winterNamePtr
                }
            }

            function _localtime_r(time, tmPtr) {
                _tzset();
                var date = new Date(HEAP32[time >> 2] * 1e3);
                HEAP32[tmPtr >> 2] = date.getSeconds();
                HEAP32[tmPtr + 4 >> 2] = date.getMinutes();
                HEAP32[tmPtr + 8 >> 2] = date.getHours();
                HEAP32[tmPtr + 12 >> 2] = date.getDate();
                HEAP32[tmPtr + 16 >> 2] = date.getMonth();
                HEAP32[tmPtr + 20 >> 2] = date.getFullYear() - 1900;
                HEAP32[tmPtr + 24 >> 2] = date.getDay();
                var start = new Date(date.getFullYear(), 0, 1);
                var yday = (date.getTime() - start.getTime()) / (1e3 * 60 * 60 * 24) | 0;
                HEAP32[tmPtr + 28 >> 2] = yday;
                HEAP32[tmPtr + 36 >> 2] = -(date.getTimezoneOffset() * 60);
                var summerOffset = new Date(date.getFullYear(), 6, 1).getTimezoneOffset();
                var winterOffset = start.getTimezoneOffset();
                var dst = (summerOffset != winterOffset && date.getTimezoneOffset() == Math.min(winterOffset, summerOffset)) | 0;
                HEAP32[tmPtr + 32 >> 2] = dst;
                var zonePtr = HEAP32[__get_tzname() + (dst ? 4 : 0) >> 2];
                HEAP32[tmPtr + 40 >> 2] = zonePtr;
                return tmPtr
            }

            var MONO = {
                pump_count: 0, timeout_queue: [], spread_timers_maximum: 0, _vt_stack: [], mono_wasm_runtime_is_ready: false, mono_wasm_ignore_pdb_load_errors: true, _id_table: {}, pump_message: function () {
                    if (!this.mono_background_exec) this.mono_background_exec = Module.cwrap("mono_background_exec", null);
                    while (MONO.timeout_queue.length > 0) {
                        --MONO.pump_count;
                        MONO.timeout_queue.shift()()
                    }
                    while (MONO.pump_count > 0) {
                        --MONO.pump_count;
                        this.mono_background_exec()
                    }
                }, export_functions: function (module) {
                    module["pump_message"] = MONO.pump_message.bind(MONO);
                    module["prevent_timer_throttling"] = MONO.prevent_timer_throttling.bind(MONO);
                    module["mono_wasm_set_timeout_exec"] = MONO.mono_wasm_set_timeout_exec.bind(MONO);
                    module["mono_load_runtime_and_bcl"] = MONO.mono_load_runtime_and_bcl.bind(MONO);
                    module["mono_load_runtime_and_bcl_args"] = MONO.mono_load_runtime_and_bcl_args.bind(MONO);
                    module["mono_wasm_load_bytes_into_heap"] = MONO.mono_wasm_load_bytes_into_heap.bind(MONO);
                    module["mono_wasm_load_icu_data"] = MONO.mono_wasm_load_icu_data.bind(MONO);
                    module["mono_wasm_get_icudt_name"] = MONO.mono_wasm_get_icudt_name.bind(MONO);
                    module["mono_wasm_globalization_init"] = MONO.mono_wasm_globalization_init.bind(MONO);
                    module["mono_wasm_get_loaded_files"] = MONO.mono_wasm_get_loaded_files.bind(MONO);
                    module["mono_wasm_new_root_buffer"] = MONO.mono_wasm_new_root_buffer.bind(MONO);
                    module["mono_wasm_new_root_buffer_from_pointer"] = MONO.mono_wasm_new_root_buffer_from_pointer.bind(MONO);
                    module["mono_wasm_new_root"] = MONO.mono_wasm_new_root.bind(MONO);
                    module["mono_wasm_new_roots"] = MONO.mono_wasm_new_roots.bind(MONO);
                    module["mono_wasm_release_roots"] = MONO.mono_wasm_release_roots.bind(MONO);
                    module["mono_wasm_load_config"] = MONO.mono_wasm_load_config.bind(MONO)
                }, _base64Converter: {
                    _base64Table: ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+", "/"], _makeByteReader: function (bytes, index, count) {
                        var position = typeof index === "number" ? index : 0;
                        var endpoint;
                        if (typeof count === "number") endpoint = position + count; else endpoint = bytes.length - position;
                        var result = {
                            read: function () {
                                if (position >= endpoint) return false;
                                var nextByte = bytes[position];
                                position += 1;
                                return nextByte
                            }
                        };
                        Object.defineProperty(result, "eof", { get: function () {return position >= endpoint}, configurable: true, enumerable: true });
                        return result
                    }, toBase64StringImpl: function (inArray, offset, length) {
                        var reader = this._makeByteReader(inArray, offset, length);
                        var result = "";
                        var ch1 = 0,
                            ch2 = 0,
                            ch3 = 0,
                            bits = 0,
                            equalsCount = 0,
                            sum = 0;
                        var mask1 = (1 << 24) - 1,
                            mask2 = (1 << 18) - 1,
                            mask3 = (1 << 12) - 1,
                            mask4 = (1 << 6) - 1;
                        var shift1 = 18,
                            shift2 = 12,
                            shift3 = 6,
                            shift4 = 0;
                        while (true) {
                            ch1 = reader.read();
                            ch2 = reader.read();
                            ch3 = reader.read();
                            if (ch1 === false) break;
                            if (ch2 === false) {
                                ch2 = 0;
                                equalsCount += 1
                            }
                            if (ch3 === false) {
                                ch3 = 0;
                                equalsCount += 1
                            }
                            sum = ch1 << 16 | ch2 << 8 | ch3 << 0;
                            bits = (sum & mask1) >> shift1;
                            result += this._base64Table[bits];
                            bits = (sum & mask2) >> shift2;
                            result += this._base64Table[bits];
                            if (equalsCount < 2) {
                                bits = (sum & mask3) >> shift3;
                                result += this._base64Table[bits]
                            }
                            if (equalsCount === 2) {result += "=="} else if (equalsCount === 1) {result += "="} else {
                                bits = (sum & mask4) >> shift4;
                                result += this._base64Table[bits]
                            }
                        }
                        return result
                    }
                }, _mono_wasm_root_buffer_prototype: {
                    _throw_index_out_of_range: function () {throw new Error("index out of range")}, _check_in_range: function (index) {if (index >= this.__count || index < 0) this._throw_index_out_of_range()}, get_address: function (index) {
                        this._check_in_range(index);
                        return this.__offset + index * 4
                    }, get_address_32: function (index) {
                        this._check_in_range(index);
                        return this.__offset32 + index
                    }, get: function (index) {
                        this._check_in_range(index);
                        return Module.HEAP32[this.get_address_32(index)]
                    }, set: function (index, value) {
                        Module.HEAP32[this.get_address_32(index)] = value;
                        return value
                    }, _unsafe_get: function (index) {return Module.HEAP32[this.__offset32 + index]}, _unsafe_set: function (index, value) {Module.HEAP32[this.__offset32 + index] = value}, clear: function () {if (this.__offset) MONO._zero_region(this.__offset, this.__count * 4)}, release: function () {
                        if (this.__offset && this.__ownsAllocation) {
                            MONO.mono_wasm_deregister_root(this.__offset);
                            MONO._zero_region(this.__offset, this.__count * 4);
                            Module._free(this.__offset)
                        }
                        this.__handle = this.__offset = this.__count = this.__offset32 = 0
                    }, toString: function () {return "[root buffer @" + this.get_address(0) + ", size " + this.__count + "]"}
                }, _scratch_root_buffer: null, _scratch_root_free_indices: null, _scratch_root_free_indices_count: 0, _scratch_root_free_instances: [], _mono_wasm_root_prototype: {
                    get_address: function () {return this.__buffer.get_address(this.__index)}, get_address_32: function () {return this.__buffer.get_address_32(this.__index)}, get: function () {
                        var result = this.__buffer._unsafe_get(this.__index);
                        return result
                    }, set: function (value) {
                        this.__buffer._unsafe_set(this.__index, value);
                        return value
                    }, valueOf: function () {return this.get()}, clear: function () {this.set(0)}, release: function () {
                        const maxPooledInstances = 128;
                        if (MONO._scratch_root_free_instances.length > maxPooledInstances) {
                            MONO._mono_wasm_release_scratch_index(this.__index);
                            this.__buffer = 0;
                            this.__index = 0
                        } else {
                            this.set(0);
                            MONO._scratch_root_free_instances.push(this)
                        }
                    }, toString: function () {return "[root @" + this.get_address() + "]"}
                }, _mono_wasm_release_scratch_index: function (index) {
                    if (index === undefined) return;
                    this._scratch_root_buffer.set(index, 0);
                    this._scratch_root_free_indices[this._scratch_root_free_indices_count] = index;
                    this._scratch_root_free_indices_count++
                }, _mono_wasm_claim_scratch_index: function () {
                    if (!this._scratch_root_buffer) {
                        const maxScratchRoots = 8192;
                        this._scratch_root_buffer = this.mono_wasm_new_root_buffer(maxScratchRoots, "js roots");
                        this._scratch_root_free_indices = new Int32Array(maxScratchRoots);
                        this._scratch_root_free_indices_count = maxScratchRoots;
                        for (var i = 0; i < maxScratchRoots; i++) this._scratch_root_free_indices[i] = maxScratchRoots - i - 1;
                        Object.defineProperty(this._mono_wasm_root_prototype, "value", { get: this._mono_wasm_root_prototype.get, set: this._mono_wasm_root_prototype.set, configurable: false })
                    }
                    if (this._scratch_root_free_indices_count < 1) throw new Error("Out of scratch root space");
                    var result = this._scratch_root_free_indices[this._scratch_root_free_indices_count - 1];
                    this._scratch_root_free_indices_count--;
                    return result
                }, _zero_region: function (byteOffset, sizeBytes) {if (byteOffset % 4 === 0 && sizeBytes % 4 === 0) Module.HEAP32.fill(0, byteOffset / 4, sizeBytes / 4); else Module.HEAP8.fill(0, byteOffset, sizeBytes)}, mono_wasm_new_root_buffer: function (capacity, msg) {
                    if (!this.mono_wasm_register_root || !this.mono_wasm_deregister_root) {
                        this.mono_wasm_register_root = Module.cwrap("mono_wasm_register_root", "number", ["number", "number", "string"]);
                        this.mono_wasm_deregister_root = Module.cwrap("mono_wasm_deregister_root", null, ["number"])
                    }
                    if (capacity <= 0) throw new Error("capacity >= 1");
                    capacity = capacity | 0;
                    var capacityBytes = capacity * 4;
                    var offset = Module._malloc(capacityBytes);
                    if (offset % 4 !== 0) throw new Error("Malloc returned an unaligned offset");
                    this._zero_region(offset, capacityBytes);
                    var result = Object.create(this._mono_wasm_root_buffer_prototype);
                    result.__offset = offset;
                    result.__offset32 = offset / 4 | 0;
                    result.__count = capacity;
                    result.length = capacity;
                    result.__handle = this.mono_wasm_register_root(offset, capacityBytes, msg || 0);
                    result.__ownsAllocation = true;
                    return result
                }, mono_wasm_new_root_buffer_from_pointer: function (offset, capacity, msg) {
                    if (!this.mono_wasm_register_root || !this.mono_wasm_deregister_root) {
                        this.mono_wasm_register_root = Module.cwrap("mono_wasm_register_root", "number", ["number", "number", "string"]);
                        this.mono_wasm_deregister_root = Module.cwrap("mono_wasm_deregister_root", null, ["number"])
                    }
                    if (capacity <= 0) throw new Error("capacity >= 1");
                    capacity = capacity | 0;
                    var capacityBytes = capacity * 4;
                    if (offset % 4 !== 0) throw new Error("Unaligned offset");
                    this._zero_region(offset, capacityBytes);
                    var result = Object.create(this._mono_wasm_root_buffer_prototype);
                    result.__offset = offset;
                    result.__offset32 = offset / 4 | 0;
                    result.__count = capacity;
                    result.length = capacity;
                    result.__handle = this.mono_wasm_register_root(offset, capacityBytes, msg || 0);
                    result.__ownsAllocation = false;
                    return result
                }, mono_wasm_new_root: function (value) {
                    var result;
                    if (this._scratch_root_free_instances.length > 0) {result = this._scratch_root_free_instances.pop()} else {
                        var index = this._mono_wasm_claim_scratch_index();
                        var buffer = this._scratch_root_buffer;
                        result = Object.create(this._mono_wasm_root_prototype);
                        result.__buffer = buffer;
                        result.__index = index
                    }
                    if (value !== undefined) {
                        if (typeof value !== "number") throw new Error("value must be an address in the managed heap");
                        result.set(value)
                    } else {result.set(0)}
                    return result
                }, mono_wasm_new_roots: function (count_or_values) {
                    var result;
                    if (Array.isArray(count_or_values)) {
                        result = new Array(count_or_values.length);
                        for (var i = 0; i < result.length; i++) result[i] = this.mono_wasm_new_root(count_or_values[i])
                    } else if ((count_or_values | 0) > 0) {
                        result = new Array(count_or_values);
                        for (var i = 0; i < result.length; i++) result[i] = this.mono_wasm_new_root()
                    } else {throw new Error("count_or_values must be either an array or a number greater than 0")}
                    return result
                }, mono_wasm_release_roots: function () {
                    for (var i = 0; i < arguments.length; i++) {
                        if (!arguments[i]) continue;
                        arguments[i].release()
                    }
                }, mono_text_decoder: undefined, string_decoder: {
                    copy: function (mono_string) {
                        if (mono_string === 0) return null;
                        if (!this.mono_wasm_string_root) this.mono_wasm_string_root = MONO.mono_wasm_new_root();
                        this.mono_wasm_string_root.value = mono_string;
                        if (!this.mono_wasm_string_get_data) this.mono_wasm_string_get_data = Module.cwrap("mono_wasm_string_get_data", null, ["number", "number", "number", "number"]);
                        if (!this.mono_wasm_string_decoder_buffer) this.mono_wasm_string_decoder_buffer = Module._malloc(12);
                        let ppChars = this.mono_wasm_string_decoder_buffer + 0,
                            pLengthBytes = this.mono_wasm_string_decoder_buffer + 4,
                            pIsInterned = this.mono_wasm_string_decoder_buffer + 8;
                        this.mono_wasm_string_get_data(mono_string, ppChars, pLengthBytes, pIsInterned);
                        if (!this.mono_wasm_empty_string) this.mono_wasm_empty_string = "";
                        let result = this.mono_wasm_empty_string;
                        let lengthBytes = Module.HEAP32[pLengthBytes / 4],
                            pChars = Module.HEAP32[ppChars / 4],
                            isInterned = Module.HEAP32[pIsInterned / 4];
                        if (pLengthBytes && pChars) {
                            if (isInterned && MONO.interned_string_table && MONO.interned_string_table.has(mono_string)) {result = MONO.interned_string_table.get(mono_string)} else {
                                result = this.decode(pChars, pChars + lengthBytes, false);
                                if (isInterned) {
                                    if (!MONO.interned_string_table) MONO.interned_string_table = new Map;
                                    MONO.interned_string_table.set(mono_string, result)
                                }
                            }
                        }
                        this.mono_wasm_string_root.value = 0;
                        return result
                    }, decode: function (start, end, save) {
                        if (!MONO.mono_text_decoder) {MONO.mono_text_decoder = typeof TextDecoder !== "undefined" ? new TextDecoder("utf-16le") : undefined}
                        var str = "";
                        if (MONO.mono_text_decoder) {
                            var subArray = typeof SharedArrayBuffer !== "undefined" && Module.HEAPU8.buffer instanceof SharedArrayBuffer ? Module.HEAPU8.slice(start, end) : Module.HEAPU8.subarray(start, end);
                            str = MONO.mono_text_decoder.decode(subArray)
                        } else {
                            for (var i = 0; i < end - start; i += 2) {
                                var char = Module.getValue(start + i, "i16");
                                str += String.fromCharCode(char)
                            }
                        }
                        if (save) this.result = str;
                        return str
                    }
                }, mono_wasm_add_dbg_command_received: function (res_ok, id, buffer, buffer_len) {
                    const assembly_data = new Uint8Array(Module.HEAPU8.buffer, buffer, buffer_len);
                    const base64String = MONO._base64Converter.toBase64StringImpl(assembly_data);
                    const buffer_obj = { res_ok: res_ok, res: { id: id, value: base64String } };
                    MONO.commands_received = buffer_obj
                }, mono_wasm_malloc_and_set_debug_buffer: function (command_parameters) {
                    if (command_parameters.length > this._debugger_buffer_len) {
                        if (this._debugger_buffer) Module._free(this._debugger_buffer);
                        this._debugger_buffer_len = Math.max(command_parameters.length, this._debugger_buffer_len, 256);
                        this._debugger_buffer = Module._malloc(this._debugger_buffer_len)
                    }
                    this._debugger_heap_bytes = new Uint8Array(Module.HEAPU8.buffer, this._debugger_buffer, this._debugger_buffer_len);
                    this._debugger_heap_bytes.set(this._base64_to_uint8(command_parameters))
                }, mono_wasm_send_dbg_command_with_parms: function (id, command_set, command, command_parameters, length, valtype, newvalue) {
                    this.mono_wasm_malloc_and_set_debug_buffer(command_parameters);
                    this._c_fn_table.mono_wasm_send_dbg_command_with_parms_wrapper(id, command_set, command, this._debugger_buffer, length, valtype, newvalue.toString());
                    let { res_ok: res_ok, res: res } = MONO.commands_received;
                    if (!res_ok) throw new Error(`Failed on mono_wasm_invoke_method_debugger_agent_with_parms`);
                    return res
                }, mono_wasm_send_dbg_command: function (id, command_set, command, command_parameters) {
                    this.mono_wasm_malloc_and_set_debug_buffer(command_parameters);
                    this._c_fn_table.mono_wasm_send_dbg_command_wrapper(id, command_set, command, this._debugger_buffer, command_parameters.length);
                    let { res_ok: res_ok, res: res } = MONO.commands_received;
                    if (!res_ok) throw new Error(`Failed on mono_wasm_send_dbg_command`);
                    return res
                }, mono_wasm_get_dbg_command_info: function () {
                    let { res_ok: res_ok, res: res } = MONO.commands_received;
                    if (!res_ok) throw new Error(`Failed on mono_wasm_get_dbg_command_info`);
                    return res
                }, _get_cfo_res_details: function (objectId, args) {
                    if (!(objectId in this._call_function_res_cache)) throw new Error(`Could not find any object with id ${objectId}`);
                    const real_obj = this._call_function_res_cache[objectId];
                    const descriptors = Object.getOwnPropertyDescriptors(real_obj);
                    if (args.accessorPropertiesOnly) {Object.keys(descriptors).forEach(k => {if (descriptors[k].get === undefined) Reflect.deleteProperty(descriptors, k)})}
                    let res_details = [];
                    Object.keys(descriptors).forEach(k => {
                        let new_obj;
                        let prop_desc = descriptors[k];
                        if (typeof prop_desc.value == "object") {new_obj = Object.assign({ name: k }, prop_desc)} else if (prop_desc.value !== undefined) {new_obj = { name: k, value: Object.assign({ type: typeof prop_desc.value, description: "" + prop_desc.value }, prop_desc) }} else if (prop_desc.get !== undefined) {new_obj = { name: k, get: { className: "Function", description: `get ${k} () {}`, type: "function" } }} else {new_obj = { name: k, value: { type: "symbol", value: "<Unknown>", description: "<Unknown>" } }}
                        res_details.push(new_obj)
                    });
                    return { __value_as_json_string__: JSON.stringify(res_details) }
                }, mono_wasm_get_details: function (objectId, args = {}) {return this._get_cfo_res_details(`dotnet:cfo_res:${objectId}`, args)}, _cache_call_function_res: function (obj) {
                    const id = `dotnet:cfo_res:${this._next_call_function_res_id++}`;
                    this._call_function_res_cache[id] = obj;
                    return id
                }, mono_wasm_release_object: function (objectId) {if (objectId in this._cache_call_function_res) delete this._cache_call_function_res[objectId]}, _create_proxy_from_object_id: function (objectId, details) {
                    if (objectId.startsWith("dotnet:array:")) {
                        let ret = details.map(p => p.value);
                        return ret
                    }
                    let proxy = {};
                    Object.keys(details).forEach(p => {
                        var prop = details[p];
                        if (prop.get !== undefined) {
                            Object.defineProperty(proxy, prop.name, {
                                get() {return MONO.mono_wasm_send_dbg_command(-1, prop.get.commandSet, prop.get.command, prop.get.buffer, prop.get.length)}, set: function (newValue) {
                                    MONO.mono_wasm_send_dbg_command_with_parms(-1, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue);
                                    return MONO.commands_received.res_ok
                                }
                            })
                        } else if (prop.set !== undefined) {
                            Object.defineProperty(proxy, prop.name, {
                                get() {return prop.value}, set: function (newValue) {
                                    MONO.mono_wasm_send_dbg_command_with_parms(-1, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue);
                                    return MONO.commands_received.res_ok
                                }
                            })
                        } else {proxy[prop.name] = prop.value}
                    });
                    return proxy
                }, mono_wasm_call_function_on: function (request) {
                    if (request.arguments != undefined && !Array.isArray(request.arguments)) throw new Error(`"arguments" should be an array, but was ${request.arguments}`);
                    const objId = request.objectId;
                    const details = request.details;
                    let proxy;
                    if (objId.startsWith("dotnet:cfo_res:")) {if (objId in this._call_function_res_cache) proxy = this._call_function_res_cache[objId]; else throw new Error(`Unknown object id ${objId}`)} else {proxy = this._create_proxy_from_object_id(objId, details)}
                    const fn_args = request.arguments != undefined ? request.arguments.map(a => JSON.stringify(a.value)) : [];
                    const fn_eval_str = `var fn = ${request.functionDeclaration}; fn.call (proxy, ...[${fn_args}]);`;
                    const fn_res = eval(fn_eval_str);
                    if (fn_res === undefined) return { type: "undefined" };
                    if (Object(fn_res) !== fn_res) {
                        if (typeof fn_res == "object" && fn_res == null) return { type: typeof fn_res, subtype: `${fn_res}`, value: null };
                        return { type: typeof fn_res, description: `${fn_res}`, value: `${fn_res}` }
                    }
                    if (request.returnByValue && fn_res.subtype == undefined) return { type: "object", value: fn_res };
                    if (Object.getPrototypeOf(fn_res) == Array.prototype) {
                        const fn_res_id = this._cache_call_function_res(fn_res);
                        return { type: "object", subtype: "array", className: "Array", description: `Array(${fn_res.length})`, objectId: fn_res_id }
                    }
                    if (fn_res.value !== undefined || fn_res.subtype !== undefined) {return fn_res}
                    if (fn_res == proxy) return { type: "object", className: "Object", description: "Object", objectId: objId };
                    const fn_res_id = this._cache_call_function_res(fn_res);
                    return { type: "object", className: "Object", description: "Object", objectId: fn_res_id }
                }, _clear_per_step_state: function () {
                    this._next_id_var = 0;
                    this._id_table = {}
                }, mono_wasm_debugger_resume: function () {this._clear_per_step_state()}, mono_wasm_detach_debugger: function () {
                    if (!this.mono_wasm_set_is_debugger_attached) this.mono_wasm_set_is_debugger_attached = Module.cwrap("mono_wasm_set_is_debugger_attached", "void", ["bool"]);
                    this.mono_wasm_set_is_debugger_attached(false)
                }, _register_c_fn: function (name, ...args) {Object.defineProperty(this._c_fn_table, name + "_wrapper", { value: Module.cwrap(name, ...args) })}, _register_c_var_fn: function (name, ret_type, params) {
                    if (ret_type !== "bool") throw new Error(`Bug: Expected a C function signature that returns bool`);
                    this._register_c_fn(name, ret_type, params);
                    Object.defineProperty(this, name + "_info", {
                        value: function (...args) {
                            MONO.var_info = [];
                            const res_ok = MONO._c_fn_table[name + "_wrapper"](...args);
                            let res = MONO.var_info;
                            MONO.var_info = [];
                            if (res_ok) {
                                res = this._fixup_name_value_objects(res);
                                return { res_ok: res_ok, res: res }
                            }
                            return { res_ok: res_ok, res: undefined }
                        }
                    })
                }, mono_wasm_runtime_ready: function () {
                    this.mono_wasm_runtime_is_ready = true;
                    this._clear_per_step_state();
                    this._next_call_function_res_id = 0;
                    this._call_function_res_cache = {};
                    this._c_fn_table = {};
                    this._register_c_fn("mono_wasm_send_dbg_command", "bool", ["number", "number", "number", "number", "number"]);
                    this._register_c_fn("mono_wasm_send_dbg_command_with_parms", "bool", ["number", "number", "number", "number", "number", "number", "string"]);
                    this._debugger_buffer_len = -1;
                    if (globalThis.dotnetDebugger) debugger; else console.debug("mono_wasm_runtime_ready", "fe00e07a-5519-4dfe-b35a-f867dbaf2e28")
                }, mono_wasm_setenv: function (name, value) {
                    if (!this.wasm_setenv) this.wasm_setenv = Module.cwrap("mono_wasm_setenv", null, ["string", "string"]);
                    this.wasm_setenv(name, value)
                }, mono_wasm_set_runtime_options: function (options) {
                    if (!this.wasm_parse_runtime_options) this.wasm_parse_runtime_options = Module.cwrap("mono_wasm_parse_runtime_options", null, ["number", "number"]);
                    var argv = Module._malloc(options.length * 4);
                    var wasm_strdup = Module.cwrap("mono_wasm_strdup", "number", ["string"]);
                    let aindex = 0;
                    for (var i = 0; i < options.length; ++i) {
                        Module.setValue(argv + aindex * 4, wasm_strdup(options[i]), "i32");
                        aindex += 1
                    }
                    this.wasm_parse_runtime_options(options.length, argv)
                }, mono_wasm_init_aot_profiler: function (options) {
                    if (options == null) options = {};
                    if (!("write_at" in options)) options.write_at = "Interop/Runtime::StopProfile";
                    if (!("send_to" in options)) options.send_to = "Interop/Runtime::DumpAotProfileData";
                    var arg = "aot:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
                    Module.ccall("mono_wasm_load_profiler_aot", null, ["string"], [arg])
                }, mono_wasm_init_coverage_profiler: function (options) {
                    if (options == null) options = {};
                    if (!("write_at" in options)) options.write_at = "WebAssembly.Runtime::StopProfile";
                    if (!("send_to" in options)) options.send_to = "WebAssembly.Runtime::DumpCoverageProfileData";
                    var arg = "coverage:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
                    Module.ccall("mono_wasm_load_profiler_coverage", null, ["string"], [arg])
                }, _apply_configuration_from_args: function (args) {
                    for (var k in args.environment_variables || {}) MONO.mono_wasm_setenv(k, args.environment_variables[k]);
                    if (args.runtime_options) MONO.mono_wasm_set_runtime_options(args.runtime_options);
                    if (args.aot_profiler_options) MONO.mono_wasm_init_aot_profiler(args.aot_profiler_options);
                    if (args.coverage_profiler_options) MONO.mono_wasm_init_coverage_profiler(args.coverage_profiler_options)
                }, _get_fetch_file_cb_from_args: function (args) {
                    if (typeof args.fetch_file_cb === "function") return args.fetch_file_cb;
                    if (ENVIRONMENT_IS_NODE) {
                        var fs = {};
                        return function (asset) {
                            console.debug("MONO_WASM: Loading... " + asset);
                            var binary = fs.readFileSync(asset);
                            var resolve_func2 = function (resolve, reject) {resolve(new Uint8Array(binary))};
                            var resolve_func1 = function (resolve, reject) {
                                var response = { ok: true, url: asset, arrayBuffer: function () {return new Promise(resolve_func2)} };
                                resolve(response)
                            };
                            return new Promise(resolve_func1)
                        }
                    } else if (typeof fetch === "function") {return function (asset) {return fetch(asset, { credentials: "same-origin" })}} else {throw new Error("No fetch_file_cb was provided and this environment does not expose 'fetch'.")}
                }, _handle_loaded_asset: function (ctx, asset, url, blob) {
                    var bytes = new Uint8Array(blob);
                    if (ctx.tracing) console.log("MONO_WASM: Loaded:", asset.name, "size", bytes.length, "from", url);
                    var virtualName = asset.virtual_path || asset.name;
                    var offset = null;
                    switch (asset.behavior) {
                        case"resource":
                        case"assembly":
                            ctx.loaded_files.push({ url: url, file: virtualName });
                        case"heap":
                        case"icu":
                            offset = this.mono_wasm_load_bytes_into_heap(bytes);
                            ctx.loaded_assets[virtualName] = [offset, bytes.length];
                            break;
                        case"vfs":
                            var lastSlash = virtualName.lastIndexOf("/");
                            var parentDirectory = lastSlash > 0 ? virtualName.substr(0, lastSlash) : null;
                            var fileName = lastSlash > 0 ? virtualName.substr(lastSlash + 1) : virtualName;
                            if (fileName.startsWith("/")) fileName = fileName.substr(1);
                            if (parentDirectory) {
                                if (ctx.tracing) console.log("MONO_WASM: Creating directory '" + parentDirectory + "'");
                                var pathRet = ctx.createPath("/", parentDirectory, true, true)
                            } else {parentDirectory = "/"}
                            if (ctx.tracing) console.log("MONO_WASM: Creating file '" + fileName + "' in directory '" + parentDirectory + "'");
                            if (!this.mono_wasm_load_data_archive(bytes, parentDirectory)) {var fileRet = ctx.createDataFile(parentDirectory, fileName, bytes, true, true, true)}
                            break;
                        default:
                            throw new Error("Unrecognized asset behavior:", asset.behavior, "for asset", asset.name)
                    }
                    if (asset.behavior === "assembly") {
                        var hasPpdb = ctx.mono_wasm_add_assembly(virtualName, offset, bytes.length);
                        if (!hasPpdb) {
                            var index = ctx.loaded_files.findIndex(element => element.file == virtualName);
                            ctx.loaded_files.splice(index, 1)
                        }
                    } else if (asset.behavior === "icu") {if (this.mono_wasm_load_icu_data(offset)) ctx.num_icu_assets_loaded_successfully += 1; else console.error("Error loading ICU asset", asset.name)} else if (asset.behavior === "resource") {ctx.mono_wasm_add_satellite_assembly(virtualName, asset.culture, offset, bytes.length)}
                }, mono_load_runtime_and_bcl: function (unused_vfs_prefix, deploy_prefix, debug_level, file_list, loaded_cb, fetch_file_cb) {
                    var args = { fetch_file_cb: fetch_file_cb, loaded_cb: loaded_cb, debug_level: debug_level, assembly_root: deploy_prefix, assets: [] };
                    for (var i = 0; i < file_list.length; i++) {
                        var file_name = file_list[i];
                        var behavior;
                        if (file_name.startsWith("icudt") && file_name.endsWith(".dat")) {behavior = "icu"} else {behavior = "assembly"}
                        args.assets.push({ name: file_name, behavior: behavior })
                    }
                    return this.mono_load_runtime_and_bcl_args(args)
                }, mono_load_runtime_and_bcl_args: function (args) {
                    try {return this._load_assets_and_runtime(args)} catch (exc) {
                        console.error("error in mono_load_runtime_and_bcl_args:", exc);
                        throw exc
                    }
                }, mono_wasm_load_bytes_into_heap: function (bytes) {
                    var memoryOffset = Module._malloc(bytes.length);
                    var heapBytes = new Uint8Array(Module.HEAPU8.buffer, memoryOffset, bytes.length);
                    heapBytes.set(bytes);
                    return memoryOffset
                }, num_icu_assets_loaded_successfully: 0, mono_wasm_load_icu_data: function (offset) {
                    var fn = Module.cwrap("mono_wasm_load_icu_data", "number", ["number"]);
                    var ok = fn(offset) === 1;
                    if (ok) this.num_icu_assets_loaded_successfully++;
                    return ok
                }, mono_wasm_get_icudt_name: function (culture) {return Module.ccall("mono_wasm_get_icudt_name", "string", ["string"], [culture])}, _finalize_startup: function (args, ctx) {
                    var loaded_files_with_debug_info = [];
                    MONO.loaded_assets = ctx.loaded_assets;
                    ctx.loaded_files.forEach(value => loaded_files_with_debug_info.push(value.url));
                    MONO.loaded_files = loaded_files_with_debug_info;
                    if (ctx.tracing) {
                        console.log("MONO_WASM: loaded_assets: " + JSON.stringify(ctx.loaded_assets));
                        console.log("MONO_WASM: loaded_files: " + JSON.stringify(ctx.loaded_files))
                    }
                    var load_runtime = Module.cwrap("mono_wasm_load_runtime", null, ["string", "number"]);
                    console.debug("MONO_WASM: Initializing mono runtime");
                    this.mono_wasm_globalization_init(args.globalization_mode);
                    if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
                        try {load_runtime("unused", args.debug_level)} catch (ex) {
                            print("MONO_WASM: load_runtime () failed: " + ex);
                            print("MONO_WASM: Stacktrace: \n");
                            print(ex.stack);
                            var wasm_exit = Module.cwrap("mono_wasm_exit", null, ["number"]);
                            wasm_exit(1)
                        }
                    } else {load_runtime("unused", args.debug_level)}
                    let tz;
                    try {tz = Intl.DateTimeFormat().resolvedOptions().timeZone} catch {}
                    MONO.mono_wasm_setenv("TZ", tz || "UTC");
                    MONO.mono_wasm_runtime_ready();
                    args.loaded_cb()
                }, _load_assets_and_runtime: function (args) {
                    if (args.enable_debugging) args.debug_level = args.enable_debugging;
                    if (args.assembly_list) throw new Error("Invalid args (assembly_list was replaced by assets)");
                    if (args.runtime_assets) throw new Error("Invalid args (runtime_assets was replaced by assets)");
                    if (args.runtime_asset_sources) throw new Error("Invalid args (runtime_asset_sources was replaced by remote_sources)");
                    if (!args.loaded_cb) throw new Error("loaded_cb not provided");
                    var ctx = { tracing: args.diagnostic_tracing || false, pending_count: args.assets.length, mono_wasm_add_assembly: Module.cwrap("mono_wasm_add_assembly", "number", ["string", "number", "number"]), mono_wasm_add_satellite_assembly: Module.cwrap("mono_wasm_add_satellite_assembly", "void", ["string", "string", "number", "number"]), loaded_assets: Object.create(null), loaded_files: [], createPath: Module["FS_createPath"], createDataFile: Module["FS_createDataFile"] };
                    if (ctx.tracing) console.log("mono_wasm_load_runtime_with_args", JSON.stringify(args));
                    this._apply_configuration_from_args(args);
                    var fetch_file_cb = this._get_fetch_file_cb_from_args(args);
                    var onPendingRequestComplete = function () {
                        --ctx.pending_count;
                        if (ctx.pending_count === 0) {
                            try {MONO._finalize_startup(args, ctx)} catch (exc) {
                                console.error("Unhandled exception in _finalize_startup", exc);
                                throw exc
                            }
                        }
                    };
                    var processFetchResponseBuffer = function (asset, url, blob) {
                        try {MONO._handle_loaded_asset(ctx, asset, url, blob)} catch (exc) {
                            console.error("Unhandled exception in processFetchResponseBuffer", exc);
                            throw exc
                        } finally {onPendingRequestComplete()}
                    };
                    args.assets.forEach(function (asset) {
                        var attemptNextSource;
                        var sourceIndex = 0;
                        var sourcesList = asset.load_remote ? args.remote_sources : [""];
                        var handleFetchResponse = function (response) {
                            if (!response.ok) {
                                try {
                                    attemptNextSource();
                                    return
                                } catch (exc) {
                                    console.error("MONO_WASM: Unhandled exception in handleFetchResponse attemptNextSource for asset", asset.name, exc);
                                    throw exc
                                }
                            }
                            try {
                                var bufferPromise = response["arrayBuffer"]();
                                bufferPromise.then(processFetchResponseBuffer.bind(this, asset, response.url))
                            } catch (exc) {
                                console.error("MONO_WASM: Unhandled exception in handleFetchResponse for asset", asset.name, exc);
                                attemptNextSource()
                            }
                        };
                        attemptNextSource = function () {
                            if (sourceIndex >= sourcesList.length) {
                                var msg = "MONO_WASM: Failed to load " + asset.name;
                                try {
                                    var isOk = asset.is_optional || asset.name.match(/\.pdb$/) && MONO.mono_wasm_ignore_pdb_load_errors;
                                    if (isOk) console.debug(msg); else {
                                        console.error(msg);
                                        throw new Error(msg)
                                    }
                                } finally {onPendingRequestComplete()}
                            }
                            var sourcePrefix = sourcesList[sourceIndex];
                            sourceIndex++;
                            if (sourcePrefix === "./") sourcePrefix = "";
                            var attemptUrl;
                            if (sourcePrefix.trim() === "") {
                                if (asset.behavior === "assembly") attemptUrl = locateFile(args.assembly_root + "/" + asset.name); else if (asset.behavior === "resource") {
                                    var path = asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                                    attemptUrl = locateFile(args.assembly_root + "/" + path)
                                } else attemptUrl = asset.name
                            } else {attemptUrl = sourcePrefix + asset.name}
                            try {
                                if (asset.name === attemptUrl) {if (ctx.tracing) console.log("Attempting to fetch '%s'", attemptUrl)} else {if (ctx.tracing) console.log("Attempting to fetch '%s' for '%s'", attemptUrl, asset.name)}
                                var fetch_promise = fetch_file_cb(attemptUrl);
                                fetch_promise.then(handleFetchResponse)
                            } catch (exc) {
                                console.error("MONO_WASM: Error fetching '%s'\n%s", attemptUrl, exc);
                                attemptNextSource()
                            }
                        };
                        attemptNextSource()
                    })
                }, mono_wasm_globalization_init: function (globalization_mode) {
                    var invariantMode = false;
                    if (globalization_mode === "invariant") invariantMode = true;
                    if (!invariantMode) {
                        if (this.num_icu_assets_loaded_successfully > 0) {console.debug("MONO_WASM: ICU data archive(s) loaded, disabling invariant mode")} else if (globalization_mode !== "icu") {
                            console.debug("MONO_WASM: ICU data archive(s) not loaded, using invariant globalization mode");
                            invariantMode = true
                        } else {
                            var msg = "invariant globalization mode is inactive and no ICU data archives were loaded";
                            console.error("MONO_WASM: ERROR: " + msg);
                            throw new Error(msg)
                        }
                    }
                    if (invariantMode) this.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
                    this.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", "1")
                }, mono_wasm_get_loaded_files: function () {
                    if (!this.mono_wasm_set_is_debugger_attached) this.mono_wasm_set_is_debugger_attached = Module.cwrap("mono_wasm_set_is_debugger_attached", "void", ["bool"]);
                    this.mono_wasm_set_is_debugger_attached(true);
                    return MONO.loaded_files
                }, mono_wasm_get_loaded_asset_table: function () {return MONO.loaded_assets}, _base64_to_uint8: function (base64String) {
                    const byteCharacters = atob(base64String);
                    const byteNumbers = new Array(byteCharacters.length);
                    for (let i = 0; i < byteCharacters.length; i++) {byteNumbers[i] = byteCharacters.charCodeAt(i)}
                    return new Uint8Array(byteNumbers)
                }, mono_wasm_load_data_archive: function (data, prefix) {
                    if (data.length < 8) return false;
                    var dataview = new DataView(data.buffer);
                    var magic = dataview.getUint32(0, true);
                    if (magic != 1651270004) {return false}
                    var manifestSize = dataview.getUint32(4, true);
                    if (manifestSize == 0 || data.length < manifestSize + 8) return false;
                    var manifest;
                    try {
                        manifestContent = Module.UTF8ArrayToString(data, 8, manifestSize);
                        manifest = JSON.parse(manifestContent);
                        if (!(manifest instanceof Array)) return false
                    } catch (exc) {return false}
                    data = data.slice(manifestSize + 8);
                    var folders = new Set;
                    manifest.filter(m => {
                        var file = m[0];
                        var last = file.lastIndexOf("/");
                        var directory = file.slice(0, last + 1);
                        folders.add(directory)
                    });
                    folders.forEach(folder => {Module["FS_createPath"](prefix, folder, true, true)});
                    for (row of manifest) {
                        var name = row[0];
                        var length = row[1];
                        var bytes = data.slice(0, length);
                        Module["FS_createDataFile"](prefix, name, bytes, true, true);
                        data = data.slice(length)
                    }
                    return true
                }, mono_wasm_raise_debug_event: function (event, args = {}) {
                    if (typeof event !== "object") throw new Error(`event must be an object, but got ${JSON.stringify(event)}`);
                    if (event.eventName === undefined) throw new Error(`event.eventName is a required parameter, in event: ${JSON.stringify(event)}`);
                    if (typeof args !== "object") throw new Error(`args must be an object, but got ${JSON.stringify(args)}`);
                    console.debug("mono_wasm_debug_event_raised:aef14bca-5519-4dfe-b35a-f867abc123ae", JSON.stringify(event), JSON.stringify(args))
                }, mono_wasm_load_config: async function (configFilePath) {
                    Module.addRunDependency(configFilePath);
                    try {
                        let config = null;
                        if (ENVIRONMENT_IS_WEB) {
                            const configRaw = await fetch(configFilePath);
                            config = await configRaw.json()
                        } else if (ENVIRONMENT_IS_NODE) {config = {}} else {config = JSON.parse(read(configFilePath))}
                        Module.config = config
                    } catch (e) {Module.config = { message: "failed to load config file", error: e }} finally {Module.removeRunDependency(configFilePath)}
                }, mono_wasm_set_timeout_exec: function (id) {
                    if (!this.mono_set_timeout_exec) this.mono_set_timeout_exec = Module.cwrap("mono_set_timeout_exec", null, ["number"]);
                    this.mono_set_timeout_exec(id)
                }, prevent_timer_throttling: function () {
                    let now = (new Date).valueOf();
                    const desired_reach_time = now + 1e3 * 60 * 6;
                    const next_reach_time = Math.max(now + 1e3, this.spread_timers_maximum);
                    const light_throttling_frequency = 1e3;
                    for (var schedule = next_reach_time; schedule < desired_reach_time; schedule += light_throttling_frequency) {
                        const delay = schedule - now;
                        setTimeout(() => {
                            this.mono_wasm_set_timeout_exec(0);
                            MONO.pump_count++;
                            MONO.pump_message()
                        }, delay)
                    }
                    this.spread_timers_maximum = desired_reach_time
                }
            };

            function _mono_set_timeout(timeout, id) {
                if (typeof globalThis.setTimeout === "function") {globalThis.setTimeout(function () {MONO.mono_wasm_set_timeout_exec(id)}, timeout)} else {
                    ++MONO.pump_count;
                    MONO.timeout_queue.push(function () {MONO.mono_wasm_set_timeout_exec(id)})
                }
            }

            var BINDING = {
                BINDING_ASM: "[System.Private.Runtime.InteropServices.JavaScript]System.Runtime.InteropServices.JavaScript.Runtime", _cs_owned_objects_by_js_handle: [], _js_handle_free_list: [], _next_js_handle: 1, mono_wasm_marshal_enum_as_int: true, mono_bindings_init: function (binding_asm) {this.BINDING_ASM = binding_asm}, export_functions: function (module) {
                    module["mono_bindings_init"] = BINDING.mono_bindings_init.bind(BINDING);
                    module["mono_bind_method"] = BINDING.bind_method.bind(BINDING);
                    module["mono_method_invoke"] = BINDING.call_method.bind(BINDING);
                    module["mono_method_get_call_signature"] = BINDING.mono_method_get_call_signature.bind(BINDING);
                    module["mono_method_resolve"] = BINDING.resolve_method_fqn.bind(BINDING);
                    module["mono_bind_static_method"] = BINDING.bind_static_method.bind(BINDING);
                    module["mono_call_static_method"] = BINDING.call_static_method.bind(BINDING);
                    module["mono_bind_assembly_entry_point"] = BINDING.bind_assembly_entry_point.bind(BINDING);
                    module["mono_call_assembly_entry_point"] = BINDING.call_assembly_entry_point.bind(BINDING);
                    module["mono_intern_string"] = BINDING.mono_intern_string.bind(BINDING)
                }, bindings_lazy_init: function () {
                    if (this.init) return;
                    this.init = true;
                    this.wasm_type_symbol = Symbol.for("wasm type");
                    this.js_owned_gc_handle_symbol = Symbol.for("wasm js_owned_gc_handle");
                    this.cs_owned_js_handle_symbol = Symbol.for("wasm cs_owned_js_handle");
                    this.delegate_invoke_symbol = Symbol.for("wasm delegate_invoke");
                    this.delegate_invoke_signature_symbol = Symbol.for("wasm delegate_invoke_signature");
                    this.listener_registration_count_symbol = Symbol.for("wasm listener_registration_count");
                    Object.prototype[this.wasm_type_symbol] = 0;
                    Array.prototype[this.wasm_type_symbol] = 1;
                    ArrayBuffer.prototype[this.wasm_type_symbol] = 2;
                    DataView.prototype[this.wasm_type_symbol] = 3;
                    Function.prototype[this.wasm_type_symbol] = 4;
                    Map.prototype[this.wasm_type_symbol] = 5;
                    if (typeof SharedArrayBuffer !== "undefined") SharedArrayBuffer.prototype[this.wasm_type_symbol] = 6;
                    Int8Array.prototype[this.wasm_type_symbol] = 10;
                    Uint8Array.prototype[this.wasm_type_symbol] = 11;
                    Uint8ClampedArray.prototype[this.wasm_type_symbol] = 12;
                    Int16Array.prototype[this.wasm_type_symbol] = 13;
                    Uint16Array.prototype[this.wasm_type_symbol] = 14;
                    Int32Array.prototype[this.wasm_type_symbol] = 15;
                    Uint32Array.prototype[this.wasm_type_symbol] = 16;
                    Float32Array.prototype[this.wasm_type_symbol] = 17;
                    Float64Array.prototype[this.wasm_type_symbol] = 18;
                    this.assembly_load = Module.cwrap("mono_wasm_assembly_load", "number", ["string"]);
                    this.find_corlib_class = Module.cwrap("mono_wasm_find_corlib_class", "number", ["string", "string"]);
                    this.find_class = Module.cwrap("mono_wasm_assembly_find_class", "number", ["number", "string", "string"]);
                    this._find_method = Module.cwrap("mono_wasm_assembly_find_method", "number", ["number", "string", "number"]);
                    this.invoke_method = Module.cwrap("mono_wasm_invoke_method", "number", ["number", "number", "number", "number"]);
                    this.mono_string_get_utf8 = Module.cwrap("mono_wasm_string_get_utf8", "number", ["number"]);
                    this.mono_wasm_string_from_utf16 = Module.cwrap("mono_wasm_string_from_utf16", "number", ["number", "number"]);
                    this.mono_get_obj_type = Module.cwrap("mono_wasm_get_obj_type", "number", ["number"]);
                    this.mono_array_length = Module.cwrap("mono_wasm_array_length", "number", ["number"]);
                    this.mono_array_get = Module.cwrap("mono_wasm_array_get", "number", ["number", "number"]);
                    this.mono_obj_array_new = Module.cwrap("mono_wasm_obj_array_new", "number", ["number"]);
                    this.mono_obj_array_set = Module.cwrap("mono_wasm_obj_array_set", "void", ["number", "number", "number"]);
                    this.mono_wasm_register_bundled_satellite_assemblies = Module.cwrap("mono_wasm_register_bundled_satellite_assemblies", "void", []);
                    this.mono_wasm_try_unbox_primitive_and_get_type = Module.cwrap("mono_wasm_try_unbox_primitive_and_get_type", "number", ["number", "number"]);
                    this.mono_wasm_box_primitive = Module.cwrap("mono_wasm_box_primitive", "number", ["number", "number", "number"]);
                    this.mono_wasm_intern_string = Module.cwrap("mono_wasm_intern_string", "number", ["number"]);
                    this.assembly_get_entry_point = Module.cwrap("mono_wasm_assembly_get_entry_point", "number", ["number"]);
                    this.mono_wasm_get_delegate_invoke = Module.cwrap("mono_wasm_get_delegate_invoke", "number", ["number"]);
                    this.mono_wasm_string_array_new = Module.cwrap("mono_wasm_string_array_new", "number", ["number"]);
                    this._box_buffer = Module._malloc(16);
                    this._unbox_buffer = Module._malloc(16);
                    this._class_int32 = this.find_corlib_class("System", "Int32");
                    this._class_uint32 = this.find_corlib_class("System", "UInt32");
                    this._class_double = this.find_corlib_class("System", "Double");
                    this._class_boolean = this.find_corlib_class("System", "Boolean");
                    this.mono_typed_array_new = Module.cwrap("mono_wasm_typed_array_new", "number", ["number", "number", "number", "number"]);
                    var binding_fqn_asm = this.BINDING_ASM.substring(this.BINDING_ASM.indexOf("[") + 1, this.BINDING_ASM.indexOf("]")).trim();
                    var binding_fqn_class = this.BINDING_ASM.substring(this.BINDING_ASM.indexOf("]") + 1).trim();
                    this.binding_module = this.assembly_load(binding_fqn_asm);
                    if (!this.binding_module) throw"Can't find bindings module assembly: " + binding_fqn_asm;
                    var namespace = null,
                        classname = null;
                    if (binding_fqn_class !== null && typeof binding_fqn_class !== "undefined") {
                        namespace = "System.Runtime.InteropServices.JavaScript";
                        classname = binding_fqn_class.length > 0 ? binding_fqn_class : "Runtime";
                        if (binding_fqn_class.indexOf(".") != -1) {
                            var idx = binding_fqn_class.lastIndexOf(".");
                            namespace = binding_fqn_class.substring(0, idx);
                            classname = binding_fqn_class.substring(idx + 1)
                        }
                    }
                    var wasm_runtime_class = this.find_class(this.binding_module, namespace, classname);
                    if (!wasm_runtime_class) throw"Can't find " + binding_fqn_class + " class";
                    var get_method = function (method_name) {
                        var res = BINDING.find_method(wasm_runtime_class, method_name, -1);
                        if (!res) throw"Can't find method " + namespace + "." + classname + ":" + method_name;
                        return res
                    };
                    var bind_runtime_method = function (method_name, signature) {
                        var method = get_method(method_name);
                        return BINDING.bind_method(method, 0, signature, "BINDINGS_" + method_name)
                    };
                    this.get_call_sig = get_method("GetCallSignature");
                    this._get_cs_owned_object_by_js_handle = bind_runtime_method("GetCSOwnedObjectByJSHandle", "ii!");
                    this._get_cs_owned_object_js_handle = bind_runtime_method("GetCSOwnedObjectJSHandle", "mi");
                    this._try_get_cs_owned_object_js_handle = bind_runtime_method("TryGetCSOwnedObjectJSHandle", "mi");
                    this._create_cs_owned_proxy = bind_runtime_method("CreateCSOwnedProxy", "iii!");
                    this._get_js_owned_object_by_gc_handle = bind_runtime_method("GetJSOwnedObjectByGCHandle", "i!");
                    this._get_js_owned_object_gc_handle = bind_runtime_method("GetJSOwnedObjectGCHandle", "m");
                    this._release_js_owned_object_by_gc_handle = bind_runtime_method("ReleaseJSOwnedObjectByGCHandle", "i");
                    this._create_tcs = bind_runtime_method("CreateTaskSource", "");
                    this._set_tcs_result = bind_runtime_method("SetTaskSourceResult", "io");
                    this._set_tcs_failure = bind_runtime_method("SetTaskSourceFailure", "is");
                    this._get_tcs_task = bind_runtime_method("GetTaskSourceTask", "i!");
                    this._setup_js_cont = bind_runtime_method("SetupJSContinuation", "mo");
                    this._object_to_string = bind_runtime_method("ObjectToString", "m");
                    this._get_date_value = bind_runtime_method("GetDateValue", "m");
                    this._create_date_time = bind_runtime_method("CreateDateTime", "d!");
                    this._create_uri = bind_runtime_method("CreateUri", "s!");
                    this._is_simple_array = bind_runtime_method("IsSimpleArray", "m");
                    this._are_promises_supported = (typeof Promise === "object" || typeof Promise === "function") && typeof Promise.resolve === "function";
                    this.isThenable = (js_obj => {return Promise.resolve(js_obj) === js_obj || (typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function"});
                    this.isChromium = false;
                    if (globalThis.navigator) {
                        var nav = globalThis.navigator;
                        if (nav.userAgentData && nav.userAgentData.brands) {this.isChromium = nav.userAgentData.brands.some(i => i.brand == "Chromium")} else if (globalThis.navigator.userAgent) {this.isChromium = nav.userAgent.includes("Chrome")}
                    }
                    this._empty_string = "";
                    this._empty_string_ptr = 0;
                    this._interned_string_full_root_buffers = [];
                    this._interned_string_current_root_buffer = null;
                    this._interned_string_current_root_buffer_count = 0;
                    this._interned_js_string_table = new Map;
                    this._js_owned_object_table = new Map;
                    this._use_finalization_registry = typeof globalThis.FinalizationRegistry === "function";
                    this._use_weak_ref = typeof globalThis.WeakRef === "function";
                    if (this._use_finalization_registry) {this._js_owned_object_registry = new globalThis.FinalizationRegistry(this._js_owned_object_finalized.bind(this))}
                }, _js_owned_object_finalized: function (gc_handle) {
                    this._js_owned_object_table.delete(gc_handle);
                    this._release_js_owned_object_by_gc_handle(gc_handle)
                }, _lookup_js_owned_object: function (gc_handle) {
                    if (!gc_handle) return null;
                    var wr = this._js_owned_object_table.get(gc_handle);
                    if (wr) {return wr.deref()}
                    return null
                }, _register_js_owned_object: function (gc_handle, js_obj) {
                    var wr;
                    if (this._use_weak_ref) {wr = new WeakRef(js_obj)} else {wr = { deref: () => {return js_obj} }}
                    this._js_owned_object_table.set(gc_handle, wr)
                }, _wrap_js_thenable_as_task: function (thenable) {
                    this.bindings_lazy_init();
                    if (!thenable) return null;
                    var thenable_js_handle = BINDING.mono_wasm_get_js_handle(thenable);
                    const tcs_gc_handle = this._create_tcs();
                    thenable.then(result => {
                        this._set_tcs_result(tcs_gc_handle, result);
                        this._mono_wasm_release_js_handle(thenable_js_handle);
                        if (!this._use_finalization_registry) {this._release_js_owned_object_by_gc_handle(tcs_gc_handle)}
                    }, reason => {
                        this._set_tcs_failure(tcs_gc_handle, reason ? reason.toString() : "");
                        this._mono_wasm_release_js_handle(thenable_js_handle);
                        if (!this._use_finalization_registry) {this._release_js_owned_object_by_gc_handle(tcs_gc_handle)}
                    });
                    if (this._use_finalization_registry) {this._js_owned_object_registry.register(thenable, tcs_gc_handle)}
                    return this._get_tcs_task(tcs_gc_handle)
                }, _unbox_task_root_as_promise: function (root) {
                    this.bindings_lazy_init();
                    const self = this;
                    if (root.value === 0) return null;
                    if (!this._are_promises_supported) throw new Error("Promises are not supported thus 'System.Threading.Tasks.Task' can not work in this context.");
                    const gc_handle = this._get_js_owned_object_gc_handle(root.value);
                    var result = this._lookup_js_owned_object(gc_handle);
                    if (!result) {
                        var cont_obj = null;
                        var result = new Promise(function (resolve, reject) {
                            if (self._use_finalization_registry) {cont_obj = { resolve: resolve, reject: reject }} else {
                                cont_obj = {
                                    resolve: function () {
                                        const res = resolve.apply(null, arguments);
                                        self._js_owned_object_table.delete(gc_handle);
                                        self._release_js_owned_object_by_gc_handle(gc_handle);
                                        return res
                                    }, reject: function () {
                                        const res = reject.apply(null, arguments);
                                        self._js_owned_object_table.delete(gc_handle);
                                        self._release_js_owned_object_by_gc_handle(gc_handle);
                                        return res
                                    }
                                }
                            }
                        });
                        this._setup_js_cont(root.value, cont_obj);
                        if (this._use_finalization_registry) {this._js_owned_object_registry.register(result, gc_handle)}
                        this._register_js_owned_object(gc_handle, result)
                    }
                    return result
                }, _unbox_ref_type_root_as_js_object: function (root) {
                    this.bindings_lazy_init();
                    if (root.value === 0) return null;
                    var js_handle = this._try_get_cs_owned_object_js_handle(root.value, false);
                    if (js_handle) {
                        if (js_handle === -1) {throw new Error("Cannot access a disposed JSObject at " + root.value)}
                        return this.mono_wasm_get_jsobj_from_js_handle(js_handle)
                    }
                    const gc_handle = this._get_js_owned_object_gc_handle(root.value);
                    var result = this._lookup_js_owned_object(gc_handle);
                    if (!result) {
                        result = {};
                        result[BINDING.js_owned_gc_handle_symbol] = gc_handle;
                        if (this._use_finalization_registry) {this._js_owned_object_registry.register(result, gc_handle)}
                        this._register_js_owned_object(gc_handle, result)
                    }
                    return result
                }, _wrap_delegate_root_as_function: function (root) {
                    this.bindings_lazy_init();
                    if (root.value === 0) return null;
                    const gc_handle = this._get_js_owned_object_gc_handle(root.value);
                    return this._wrap_delegate_gc_handle_as_function(gc_handle)
                }, _wrap_delegate_gc_handle_as_function: function (gc_handle, after_listener_callback) {
                    this.bindings_lazy_init();
                    var result = this._lookup_js_owned_object(gc_handle);
                    if (!result) {
                        result = function () {
                            const delegateRoot = MONO.mono_wasm_new_root(BINDING.get_js_owned_object_by_gc_handle(gc_handle));
                            try {
                                const res = BINDING.call_method(result[BINDING.delegate_invoke_symbol], delegateRoot.value, result[BINDING.delegate_invoke_signature_symbol], arguments);
                                if (after_listener_callback) {after_listener_callback()}
                                return res
                            } finally {delegateRoot.release()}
                        };
                        const delegateRoot = MONO.mono_wasm_new_root(BINDING.get_js_owned_object_by_gc_handle(gc_handle));
                        try {
                            if (typeof result[BINDING.delegate_invoke_symbol] === "undefined") {
                                result[BINDING.delegate_invoke_symbol] = BINDING.mono_wasm_get_delegate_invoke(delegateRoot.value);
                                if (!result[BINDING.delegate_invoke_symbol]) {throw new Error("System.Delegate Invoke method can not be resolved.")}
                            }
                            if (typeof result[BINDING.delegate_invoke_signature_symbol] === "undefined") {result[BINDING.delegate_invoke_signature_symbol] = Module.mono_method_get_call_signature(result[BINDING.delegate_invoke_symbol], delegateRoot.value)}
                        } finally {delegateRoot.release()}
                        if (this._use_finalization_registry) {this._js_owned_object_registry.register(result, gc_handle)}
                        this._register_js_owned_object(gc_handle, result)
                    }
                    return result
                }, mono_intern_string: function (string) {
                    if (string.length === 0) return this._empty_string;
                    var ptr = this.js_string_to_mono_string_interned(string);
                    var result = MONO.interned_string_table.get(ptr);
                    return result
                }, _store_string_in_intern_table: function (string, ptr, internIt) {
                    if (!ptr) throw new Error("null pointer passed to _store_string_in_intern_table"); else if (typeof ptr !== "number") throw new Error(`non-pointer passed to _store_string_in_intern_table: ${typeof ptr}`);
                    const internBufferSize = 8192;
                    if (this._interned_string_current_root_buffer_count >= internBufferSize) {
                        this._interned_string_full_root_buffers.push(this._interned_string_current_root_buffer);
                        this._interned_string_current_root_buffer = null
                    }
                    if (!this._interned_string_current_root_buffer) {
                        this._interned_string_current_root_buffer = MONO.mono_wasm_new_root_buffer(internBufferSize, "interned strings");
                        this._interned_string_current_root_buffer_count = 0
                    }
                    var rootBuffer = this._interned_string_current_root_buffer;
                    var index = this._interned_string_current_root_buffer_count++;
                    rootBuffer.set(index, ptr);
                    if (internIt) rootBuffer.set(index, ptr = this.mono_wasm_intern_string(ptr));
                    if (!ptr) throw new Error("mono_wasm_intern_string produced a null pointer");
                    this._interned_js_string_table.set(string, ptr);
                    if (!MONO.interned_string_table) MONO.interned_string_table = new Map;
                    MONO.interned_string_table.set(ptr, string);
                    if (string.length === 0 && !this._empty_string_ptr) this._empty_string_ptr = ptr;
                    return ptr
                }, js_string_to_mono_string_interned: function (string) {
                    var text = typeof string === "symbol" ? string.description || Symbol.keyFor(string) || "<unknown Symbol>" : string;
                    if (text.length === 0 && this._empty_string_ptr) return this._empty_string_ptr;
                    var ptr = this._interned_js_string_table.get(string);
                    if (ptr) return ptr;
                    ptr = this.js_string_to_mono_string_new(text);
                    ptr = this._store_string_in_intern_table(string, ptr, true);
                    return ptr
                }, js_string_to_mono_string: function (string) {
                    if (string === null) return null; else if (typeof string === "symbol") return this.js_string_to_mono_string_interned(string); else if (typeof string !== "string") throw new Error("Expected string argument, got " + typeof string);
                    if (string.length === 0) return this.js_string_to_mono_string_interned(string);
                    if (string.length <= 256) {
                        var interned = this._interned_js_string_table.get(string);
                        if (interned) return interned
                    }
                    return this.js_string_to_mono_string_new(string)
                }, js_string_to_mono_string_new: function (string) {
                    var buffer = Module._malloc((string.length + 1) * 2);
                    var buffer16 = buffer / 2 | 0;
                    for (var i = 0; i < string.length; i++) Module.HEAP16[buffer16 + i] = string.charCodeAt(i);
                    Module.HEAP16[buffer16 + string.length] = 0;
                    var result = this.mono_wasm_string_from_utf16(buffer, string.length);
                    Module._free(buffer);
                    return result
                }, find_method: function (klass, name, n) {
                    var result = this._find_method(klass, name, n);
                    if (result) {
                        if (!this._method_descriptions) this._method_descriptions = new Map;
                        this._method_descriptions.set(result, name)
                    }
                    return result
                }, get_js_obj: function (js_handle) {
                    if (js_handle > 0) return this.mono_wasm_get_jsobj_from_js_handle(js_handle);
                    return null
                }, _get_string_from_intern_table: function (mono_obj) {
                    if (!MONO.interned_string_table) return undefined;
                    return MONO.interned_string_table.get(mono_obj)
                }, conv_string: function (mono_obj) {return MONO.string_decoder.copy(mono_obj)}, is_nested_array: function (ele) {return this._is_simple_array(ele)}, mono_array_to_js_array: function (mono_array) {
                    if (mono_array === 0) return null;
                    var arrayRoot = MONO.mono_wasm_new_root(mono_array);
                    try {return this._mono_array_root_to_js_array(arrayRoot)} finally {arrayRoot.release()}
                }, _mono_array_root_to_js_array: function (arrayRoot) {
                    if (arrayRoot.value === 0) return null;
                    let elemRoot = MONO.mono_wasm_new_root();
                    try {
                        var len = this.mono_array_length(arrayRoot.value);
                        var res = new Array(len);
                        for (var i = 0; i < len; ++i) {
                            elemRoot.value = this.mono_array_get(arrayRoot.value, i);
                            if (this.is_nested_array(elemRoot.value)) res[i] = this._mono_array_root_to_js_array(elemRoot); else res[i] = this._unbox_mono_obj_root(elemRoot)
                        }
                    } finally {elemRoot.release()}
                    return res
                }, js_array_to_mono_array: function (js_array, asString, should_add_in_flight) {
                    var mono_array = asString ? this.mono_wasm_string_array_new(js_array.length) : this.mono_obj_array_new(js_array.length);
                    let [arrayRoot, elemRoot] = MONO.mono_wasm_new_roots([mono_array, 0]);
                    try {
                        for (var i = 0; i < js_array.length; ++i) {
                            var obj = js_array[i];
                            if (asString) obj = obj.toString();
                            elemRoot.value = this._js_to_mono_obj(should_add_in_flight, obj);
                            this.mono_obj_array_set(arrayRoot.value, i, elemRoot.value)
                        }
                        return mono_array
                    } finally {MONO.mono_wasm_release_roots(arrayRoot, elemRoot)}
                }, js_to_mono_obj: function (js_obj) {return this._js_to_mono_obj(false, js_obj)}, unbox_mono_obj: function (mono_obj) {
                    if (mono_obj === 0) return undefined;
                    var root = MONO.mono_wasm_new_root(mono_obj);
                    try {return this._unbox_mono_obj_root(root)} finally {root.release()}
                }, _unbox_cs_owned_root_as_js_object: function (root) {
                    var js_handle = this._get_cs_owned_object_js_handle(root.value, false);
                    var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle(js_handle);
                    return js_obj
                }, _unbox_mono_obj_root_with_known_nonprimitive_type: function (root, type) {
                    if (root.value === undefined) throw new Error(`Expected a root but got ${root}`);
                    switch (type) {
                        case 26:
                        case 27:
                            throw new Error("int64 not available");
                        case 3:
                        case 29:
                            return this.conv_string(root.value);
                        case 4:
                            throw new Error("no idea on how to unbox value types");
                        case 5:
                            return this._wrap_delegate_root_as_function(root);
                        case 6:
                            return this._unbox_task_root_as_promise(root);
                        case 7:
                            return this._unbox_ref_type_root_as_js_object(root);
                        case 10:
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                        case 15:
                        case 16:
                        case 17:
                        case 18:
                            throw new Error("Marshalling of primitive arrays are not supported.  Use the corresponding TypedArray instead.");
                        case 20:
                            var dateValue = this._get_date_value(root.value);
                            return new Date(dateValue);
                        case 21:
                            var dateoffsetValue = this._object_to_string(root.value);
                            return dateoffsetValue;
                        case 22:
                            var uriValue = this._object_to_string(root.value);
                            return uriValue;
                        case 23:
                            return this._unbox_cs_owned_root_as_js_object(root);
                        case 30:
                            return undefined;
                        default:
                            throw new Error(`no idea on how to unbox object kind ${type} at offset ${root.value} (root address is ${root.get_address()})`)
                    }
                }, _unbox_mono_obj_root: function (root) {
                    if (root.value === 0) return undefined;
                    var type = this.mono_wasm_try_unbox_primitive_and_get_type(root.value, this._unbox_buffer);
                    switch (type) {
                        case 1:
                            return Module.HEAP32[this._unbox_buffer / 4];
                        case 25:
                            return Module.HEAPU32[this._unbox_buffer / 4];
                        case 24:
                            return Module.HEAPF32[this._unbox_buffer / 4];
                        case 2:
                            return Module.HEAPF64[this._unbox_buffer / 8];
                        case 8:
                            return Module.HEAP32[this._unbox_buffer / 4] !== 0;
                        case 28:
                            return String.fromCharCode(Module.HEAP32[this._unbox_buffer / 4]);
                        default:
                            return this._unbox_mono_obj_root_with_known_nonprimitive_type(root, type)
                    }
                }, js_typedarray_to_heap: function (typedArray) {
                    var numBytes = typedArray.length * typedArray.BYTES_PER_ELEMENT;
                    var ptr = Module._malloc(numBytes);
                    var heapBytes = new Uint8Array(Module.HEAPU8.buffer, ptr, numBytes);
                    heapBytes.set(new Uint8Array(typedArray.buffer, typedArray.byteOffset, numBytes));
                    return heapBytes
                }, _box_js_int: function (js_obj) {
                    Module.HEAP32[this._box_buffer / 4] = js_obj;
                    return this.mono_wasm_box_primitive(this._class_int32, this._box_buffer, 4)
                }, _box_js_uint: function (js_obj) {
                    Module.HEAPU32[this._box_buffer / 4] = js_obj;
                    return this.mono_wasm_box_primitive(this._class_uint32, this._box_buffer, 4)
                }, _box_js_double: function (js_obj) {
                    Module.HEAPF64[this._box_buffer / 8] = js_obj;
                    return this.mono_wasm_box_primitive(this._class_double, this._box_buffer, 8)
                }, _box_js_bool: function (js_obj) {
                    Module.HEAP32[this._box_buffer / 4] = js_obj ? 1 : 0;
                    return this.mono_wasm_box_primitive(this._class_boolean, this._box_buffer, 4)
                }, _js_to_mono_uri: function (should_add_in_flight, js_obj) {
                    this.bindings_lazy_init();
                    switch (true) {
                        case js_obj === null:
                        case typeof js_obj === "undefined":
                            return 0;
                        case typeof js_obj === "symbol":
                        case typeof js_obj === "string":
                            return this._create_uri(js_obj);
                        default:
                            return this._extract_mono_obj(should_add_in_flight, js_obj)
                    }
                }, _js_to_mono_obj: function (should_add_in_flight, js_obj) {
                    this.bindings_lazy_init();
                    switch (true) {
                        case js_obj === null:
                        case typeof js_obj === "undefined":
                            return 0;
                        case typeof js_obj === "number": {
                            if ((js_obj | 0) === js_obj) result = this._box_js_int(js_obj); else if (js_obj >>> 0 === js_obj) result = this._box_js_uint(js_obj); else result = this._box_js_double(js_obj);
                            if (!result) throw new Error(`Boxing failed for ${js_obj}`);
                            return result
                        }
                        case typeof js_obj === "string":
                            return this.js_string_to_mono_string(js_obj);
                        case typeof js_obj === "symbol":
                            return this.js_string_to_mono_string_interned(js_obj);
                        case typeof js_obj === "boolean":
                            return this._box_js_bool(js_obj);
                        case this.isThenable(js_obj) === true:
                            return this._wrap_js_thenable_as_task(js_obj);
                        case js_obj.constructor.name === "Date":
                            return this._create_date_time(js_obj.getTime());
                        default:
                            return this._extract_mono_obj(should_add_in_flight, js_obj)
                    }
                }, _extract_mono_obj: function (should_add_in_flight, js_obj) {
                    if (js_obj === null || typeof js_obj === "undefined") return 0;
                    var result = null;
                    if (js_obj[BINDING.js_owned_gc_handle_symbol]) {
                        result = this.get_js_owned_object_by_gc_handle(js_obj[BINDING.js_owned_gc_handle_symbol]);
                        return result
                    }
                    if (js_obj[BINDING.cs_owned_js_handle_symbol]) {
                        result = this.get_cs_owned_object_by_js_handle(js_obj[BINDING.cs_owned_js_handle_symbol], should_add_in_flight);
                        if (!result) {delete js_obj[BINDING.cs_owned_js_handle_symbol]}
                    }
                    if (!result) {
                        const wasm_type = js_obj[this.wasm_type_symbol];
                        const wasm_type_id = typeof wasm_type === "undefined" ? 0 : wasm_type;
                        var js_handle = BINDING.mono_wasm_get_js_handle(js_obj);
                        result = this._create_cs_owned_proxy(js_handle, wasm_type_id, should_add_in_flight)
                    }
                    return result
                }, has_backing_array_buffer: function (js_obj) {return typeof SharedArrayBuffer !== "undefined" ? js_obj.buffer instanceof ArrayBuffer || js_obj.buffer instanceof SharedArrayBuffer : js_obj.buffer instanceof ArrayBuffer}, js_typed_array_to_array: function (js_obj) {
                    if (!!(this.has_backing_array_buffer(js_obj) && js_obj.BYTES_PER_ELEMENT)) {
                        var arrayType = js_obj[this.wasm_type_symbol];
                        var heapBytes = this.js_typedarray_to_heap(js_obj);
                        var bufferArray = this.mono_typed_array_new(heapBytes.byteOffset, js_obj.length, js_obj.BYTES_PER_ELEMENT, arrayType);
                        Module._free(heapBytes.byteOffset);
                        return bufferArray
                    } else {throw new Error("Object '" + js_obj + "' is not a typed array")}
                }, typedarray_copy_to: function (typed_array, pinned_array, begin, end, bytes_per_element) {
                    if (!!(this.has_backing_array_buffer(typed_array) && typed_array.BYTES_PER_ELEMENT)) {
                        if (bytes_per_element !== typed_array.BYTES_PER_ELEMENT) throw new Error("Inconsistent element sizes: TypedArray.BYTES_PER_ELEMENT '" + typed_array.BYTES_PER_ELEMENT + "' sizeof managed element: '" + bytes_per_element + "'");
                        var num_of_bytes = (end - begin) * bytes_per_element;
                        var view_bytes = typed_array.length * typed_array.BYTES_PER_ELEMENT;
                        if (num_of_bytes > view_bytes) num_of_bytes = view_bytes;
                        var offset = begin * bytes_per_element;
                        var heapBytes = new Uint8Array(Module.HEAPU8.buffer, pinned_array + offset, num_of_bytes);
                        heapBytes.set(new Uint8Array(typed_array.buffer, typed_array.byteOffset, num_of_bytes));
                        return num_of_bytes
                    } else {throw new Error("Object '" + typed_array + "' is not a typed array")}
                }, typedarray_copy_from: function (typed_array, pinned_array, begin, end, bytes_per_element) {
                    if (!!(this.has_backing_array_buffer(typed_array) && typed_array.BYTES_PER_ELEMENT)) {
                        if (bytes_per_element !== typed_array.BYTES_PER_ELEMENT) throw new Error("Inconsistent element sizes: TypedArray.BYTES_PER_ELEMENT '" + typed_array.BYTES_PER_ELEMENT + "' sizeof managed element: '" + bytes_per_element + "'");
                        var num_of_bytes = (end - begin) * bytes_per_element;
                        var view_bytes = typed_array.length * typed_array.BYTES_PER_ELEMENT;
                        if (num_of_bytes > view_bytes) num_of_bytes = view_bytes;
                        var typedarrayBytes = new Uint8Array(typed_array.buffer, 0, num_of_bytes);
                        var offset = begin * bytes_per_element;
                        typedarrayBytes.set(Module.HEAPU8.subarray(pinned_array + offset, pinned_array + offset + num_of_bytes));
                        return num_of_bytes
                    } else {throw new Error("Object '" + typed_array + "' is not a typed array")}
                }, typed_array_from: function (pinned_array, begin, end, bytes_per_element, type) {
                    var newTypedArray = 0;
                    switch (type) {
                        case 5:
                            newTypedArray = new Int8Array(end - begin);
                            break;
                        case 6:
                            newTypedArray = new Uint8Array(end - begin);
                            break;
                        case 7:
                            newTypedArray = new Int16Array(end - begin);
                            break;
                        case 8:
                            newTypedArray = new Uint16Array(end - begin);
                            break;
                        case 9:
                            newTypedArray = new Int32Array(end - begin);
                            break;
                        case 10:
                            newTypedArray = new Uint32Array(end - begin);
                            break;
                        case 13:
                            newTypedArray = new Float32Array(end - begin);
                            break;
                        case 14:
                            newTypedArray = new Float64Array(end - begin);
                            break;
                        case 15:
                            newTypedArray = new Uint8ClampedArray(end - begin);
                            break
                    }
                    this.typedarray_copy_from(newTypedArray, pinned_array, begin, end, bytes_per_element);
                    return newTypedArray
                }, js_to_mono_enum: function (js_obj, method, parmIdx) {
                    this.bindings_lazy_init();
                    if (typeof js_obj !== "number") throw new Error(`Expected numeric value for enum argument, got '${js_obj}'`);
                    return js_obj | 0
                }, get_js_owned_object_by_gc_handle: function (gc_handle) {
                    if (!gc_handle) {return 0}
                    return this._get_js_owned_object_by_gc_handle(gc_handle)
                }, get_cs_owned_object_by_js_handle: function (js_handle, should_add_in_flight) {
                    if (!js_handle) {return 0}
                    return this._get_cs_owned_object_by_js_handle(js_handle, should_add_in_flight)
                }, mono_method_get_call_signature: function (method, mono_obj) {
                    let instanceRoot = MONO.mono_wasm_new_root(mono_obj);
                    try {
                        this.bindings_lazy_init();
                        return this.call_method(this.get_call_sig, null, "im", [method, instanceRoot.value])
                    } finally {instanceRoot.release()}
                }, _create_named_function: function (name, argumentNames, body, closure) {
                    var result = null,
                        closureArgumentList = null,
                        closureArgumentNames = null;
                    if (closure) {
                        closureArgumentNames = Object.keys(closure);
                        closureArgumentList = new Array(closureArgumentNames.length);
                        for (var i = 0, l = closureArgumentNames.length; i < l; i++) closureArgumentList[i] = closure[closureArgumentNames[i]]
                    }
                    var constructor = this._create_rebindable_named_function(name, argumentNames, body, closureArgumentNames);
                    result = constructor.apply(null, closureArgumentList);
                    return result
                }, _create_rebindable_named_function: function (name, argumentNames, body, closureArgNames) {
                    var strictPrefix = '"use strict";\r\n';
                    var uriPrefix = "",
                        escapedFunctionIdentifier = "";
                    if (name) {
                        uriPrefix = "//# sourceURL=https://mono-wasm.invalid/" + name + "\r\n";
                        escapedFunctionIdentifier = name
                    } else {escapedFunctionIdentifier = "unnamed"}
                    var rawFunctionText = "function " + escapedFunctionIdentifier + "(" + argumentNames.join(", ") + ") {\r\n" + body + "\r\n};\r\n";
                    var lineBreakRE = /\r(\n?)/g;
                    rawFunctionText = uriPrefix + strictPrefix + rawFunctionText.replace(lineBreakRE, "\r\n    ") + `    return ${escapedFunctionIdentifier};\r\n`;
                    var result = null,
                        keys = null;
                    if (closureArgNames) {keys = closureArgNames.concat([rawFunctionText])} else {keys = [rawFunctionText]}
                    result = Function.apply(Function, keys);
                    return result
                }, _create_primitive_converters: function () {
                    var result = new Map;
                    result.set("m", { steps: [{}], size: 0 });
                    result.set("s", { steps: [{ convert: this.js_string_to_mono_string.bind(this) }], size: 0, needs_root: true });
                    result.set("S", { steps: [{ convert: this.js_string_to_mono_string_interned.bind(this) }], size: 0, needs_root: true });
                    result.set("o", { steps: [{ convert: this._js_to_mono_obj.bind(this, false) }], size: 0, needs_root: true });
                    result.set("u", { steps: [{ convert: this._js_to_mono_uri.bind(this, false) }], size: 0, needs_root: true });
                    result.set("j", { steps: [{ convert: this.js_to_mono_enum.bind(this), indirect: "i32" }], size: 8 });
                    result.set("i", { steps: [{ indirect: "i32" }], size: 8 });
                    result.set("l", { steps: [{ indirect: "i64" }], size: 8 });
                    result.set("f", { steps: [{ indirect: "float" }], size: 8 });
                    result.set("d", { steps: [{ indirect: "double" }], size: 8 });
                    this._primitive_converters = result;
                    return result
                }, _create_converter_for_marshal_string: function (args_marshal) {
                    var primitiveConverters = this._primitive_converters;
                    if (!primitiveConverters) primitiveConverters = this._create_primitive_converters();
                    var steps = [];
                    var size = 0;
                    var is_result_definitely_unmarshaled = false,
                        is_result_possibly_unmarshaled = false,
                        result_unmarshaled_if_argc = -1,
                        needs_root_buffer = false;
                    for (var i = 0; i < args_marshal.length; ++i) {
                        var key = args_marshal[i];
                        if (i === args_marshal.length - 1) {
                            if (key === "!") {
                                is_result_definitely_unmarshaled = true;
                                continue
                            } else if (key === "m") {
                                is_result_possibly_unmarshaled = true;
                                result_unmarshaled_if_argc = args_marshal.length - 1
                            }
                        } else if (key === "!") throw new Error("! must be at the end of the signature");
                        var conv = primitiveConverters.get(key);
                        if (!conv) throw new Error("Unknown parameter type " + type);
                        var localStep = Object.create(conv.steps[0]);
                        localStep.size = conv.size;
                        if (conv.needs_root) needs_root_buffer = true;
                        localStep.needs_root = conv.needs_root;
                        localStep.key = args_marshal[i];
                        steps.push(localStep);
                        size += conv.size
                    }
                    return { steps: steps, size: size, args_marshal: args_marshal, is_result_definitely_unmarshaled: is_result_definitely_unmarshaled, is_result_possibly_unmarshaled: is_result_possibly_unmarshaled, result_unmarshaled_if_argc: result_unmarshaled_if_argc, needs_root_buffer: needs_root_buffer }
                }, _get_converter_for_marshal_string: function (args_marshal) {
                    if (!this._signature_converters) this._signature_converters = new Map;
                    var converter = this._signature_converters.get(args_marshal);
                    if (!converter) {
                        converter = this._create_converter_for_marshal_string(args_marshal);
                        this._signature_converters.set(args_marshal, converter)
                    }
                    return converter
                }, _compile_converter_for_marshal_string: function (args_marshal) {
                    var converter = this._get_converter_for_marshal_string(args_marshal);
                    if (typeof converter.args_marshal !== "string") throw new Error("Corrupt converter for '" + args_marshal + "'");
                    if (converter.compiled_function && converter.compiled_variadic_function) return converter;
                    var converterName = args_marshal.replace("!", "_result_unmarshaled");
                    converter.name = converterName;
                    var body = [];
                    var argumentNames = ["buffer", "rootBuffer", "method"];
                    var bufferSizeBytes = converter.size + args_marshal.length * 4 + 16;
                    var rootBufferSize = args_marshal.length;
                    var indirectBaseOffset = ((args_marshal.length * 4 + 7) / 8 | 0) * 8;
                    var closure = {};
                    var indirectLocalOffset = 0;
                    body.push(`if (!buffer) buffer = Module._malloc (${bufferSizeBytes});`, `var indirectStart = buffer + ${indirectBaseOffset};`, "var indirect32 = (indirectStart / 4) | 0, indirect64 = (indirectStart / 8) | 0;", "var buffer32 = (buffer / 4) | 0;", "");
                    for (let i = 0; i < converter.steps.length; i++) {
                        var step = converter.steps[i];
                        var closureKey = "step" + i;
                        var valueKey = "value" + i;
                        var argKey = "arg" + i;
                        argumentNames.push(argKey);
                        if (step.convert) {
                            closure[closureKey] = step.convert;
                            body.push(`var ${valueKey} = ${closureKey}(${argKey}, method, ${i});`)
                        } else {body.push(`var ${valueKey} = ${argKey};`)}
                        if (step.needs_root) body.push(`rootBuffer.set (${i}, ${valueKey});`);
                        if (step.indirect) {
                            var heapArrayName = null;
                            switch (step.indirect) {
                                case"u32":
                                    heapArrayName = "HEAPU32";
                                    break;
                                case"i32":
                                    heapArrayName = "HEAP32";
                                    break;
                                case"float":
                                    heapArrayName = "HEAPF32";
                                    break;
                                case"double":
                                    body.push(`Module.HEAPF64[indirect64 + ${indirectLocalOffset / 8}] = ${valueKey};`);
                                    break;
                                case"i64":
                                    body.push(`Module.setValue (indirectStart + ${indirectLocalOffset}, ${valueKey}, 'i64');`);
                                    break;
                                default:
                                    throw new Error("Unimplemented indirect type: " + step.indirect)
                            }
                            if (heapArrayName) body.push(`Module.${heapArrayName}[indirect32 + ${indirectLocalOffset / 4}] = ${valueKey};`);
                            body.push(`Module.HEAP32[buffer32 + ${i}] = indirectStart + ${indirectLocalOffset};`, "");
                            indirectLocalOffset += step.size
                        } else {
                            body.push(`Module.HEAP32[buffer32 + ${i}] = ${valueKey};`, "");
                            indirectLocalOffset += 4
                        }
                    }
                    body.push("return buffer;");
                    var bodyJs = body.join("\r\n"),
                        compiledFunction = null,
                        compiledVariadicFunction = null;
                    try {
                        compiledFunction = this._create_named_function("converter_" + converterName, argumentNames, bodyJs, closure);
                        converter.compiled_function = compiledFunction
                    } catch (exc) {
                        converter.compiled_function = null;
                        console.warn("compiling converter failed for", bodyJs, "with error", exc);
                        throw exc
                    }
                    argumentNames = ["existingBuffer", "rootBuffer", "method", "args"];
                    closure = { converter: compiledFunction };
                    body = ["return converter(", "  existingBuffer, rootBuffer, method,"];
                    for (let i = 0; i < converter.steps.length; i++) {body.push("  args[" + i + (i == converter.steps.length - 1 ? "]" : "], "))}
                    body.push(");");
                    bodyJs = body.join("\r\n");
                    try {
                        compiledVariadicFunction = this._create_named_function("variadic_converter_" + converterName, argumentNames, bodyJs, closure);
                        converter.compiled_variadic_function = compiledVariadicFunction
                    } catch (exc) {
                        converter.compiled_variadic_function = null;
                        console.warn("compiling converter failed for", bodyJs, "with error", exc);
                        throw exc
                    }
                    converter.scratchRootBuffer = null;
                    converter.scratchBuffer = 0 | 0;
                    return converter
                }, _verify_args_for_method_call: function (args_marshal, args) {
                    var has_args = args && typeof args === "object" && args.length > 0;
                    var has_args_marshal = typeof args_marshal === "string";
                    if (has_args) {if (!has_args_marshal) throw new Error("No signature provided for method call."); else if (args.length > args_marshal.length) throw new Error("Too many parameter values. Expected at most " + args_marshal.length + " value(s) for signature " + args_marshal)}
                    return has_args_marshal && has_args
                }, _get_buffer_for_method_call: function (converter) {
                    if (!converter) return 0;
                    var result = converter.scratchBuffer;
                    converter.scratchBuffer = 0;
                    return result
                }, _get_args_root_buffer_for_method_call: function (converter) {
                    if (!converter) return null;
                    if (!converter.needs_root_buffer) return null;
                    var result;
                    if (converter.scratchRootBuffer) {
                        result = converter.scratchRootBuffer;
                        converter.scratchRootBuffer = null
                    } else {
                        result = MONO.mono_wasm_new_root_buffer(converter.steps.length);
                        result.converter = converter
                    }
                    return result
                }, _release_args_root_buffer_from_method_call: function (converter, argsRootBuffer) {
                    if (!argsRootBuffer || !converter) return;
                    if (!converter.scratchRootBuffer) {
                        argsRootBuffer.clear();
                        converter.scratchRootBuffer = argsRootBuffer
                    } else {argsRootBuffer.release()}
                }, _release_buffer_from_method_call: function (converter, buffer) {
                    if (!converter || !buffer) return;
                    if (!converter.scratchBuffer) converter.scratchBuffer = buffer | 0; else Module._free(buffer | 0)
                }, _convert_exception_for_method_call: function (result, exception) {
                    if (exception === 0) return null;
                    var msg = this.conv_string(result);
                    var err = new Error(msg);
                    return err
                }, _maybe_produce_signature_warning: function (converter) {
                    if (converter.has_warned_about_signature) return;
                    console.warn("MONO_WASM: Deprecated raw return value signature: '" + converter.args_marshal + "'. End the signature with '!' instead of 'm'.");
                    converter.has_warned_about_signature = true
                }, _decide_if_result_is_marshaled: function (converter, argc) {
                    if (!converter) return true;
                    if (converter.is_result_possibly_unmarshaled && argc === converter.result_unmarshaled_if_argc) {
                        if (argc < converter.result_unmarshaled_if_argc) throw new Error(["Expected >= ", converter.result_unmarshaled_if_argc, "argument(s) but got", argc, "for signature " + converter.args_marshal].join(" "));
                        this._maybe_produce_signature_warning(converter);
                        return false
                    } else {
                        if (argc < converter.steps.length) throw new Error(["Expected", converter.steps.length, "argument(s) but got", argc, "for signature " + converter.args_marshal].join(" "));
                        return !converter.is_result_definitely_unmarshaled
                    }
                }, call_method: function (method, this_arg, args_marshal, args) {
                    this.bindings_lazy_init();
                    this_arg = this_arg | 0;
                    if ((method | 0) !== method) throw new Error(`method must be an address in the native heap, but was '${method}'`);
                    if (!method) throw new Error("no method specified");
                    var needs_converter = this._verify_args_for_method_call(args_marshal, args);
                    var buffer = 0,
                        converter = null,
                        argsRootBuffer = null;
                    var is_result_marshaled = true;
                    if (needs_converter) {
                        converter = this._compile_converter_for_marshal_string(args_marshal);
                        is_result_marshaled = this._decide_if_result_is_marshaled(converter, args.length);
                        argsRootBuffer = this._get_args_root_buffer_for_method_call(converter);
                        var scratchBuffer = this._get_buffer_for_method_call(converter);
                        buffer = converter.compiled_variadic_function(scratchBuffer, argsRootBuffer, method, args)
                    }
                    return this._call_method_with_converted_args(method, this_arg, converter, buffer, is_result_marshaled, argsRootBuffer)
                }, _handle_exception_for_call: function (converter, buffer, resultRoot, exceptionRoot, argsRootBuffer) {
                    var exc = this._convert_exception_for_method_call(resultRoot.value, exceptionRoot.value);
                    if (!exc) return;
                    this._teardown_after_call(converter, buffer, resultRoot, exceptionRoot, argsRootBuffer);
                    throw exc
                }, _handle_exception_and_produce_result_for_call: function (converter, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled) {
                    this._handle_exception_for_call(converter, buffer, resultRoot, exceptionRoot, argsRootBuffer);
                    if (is_result_marshaled) result = this._unbox_mono_obj_root(resultRoot); else result = resultRoot.value;
                    this._teardown_after_call(converter, buffer, resultRoot, exceptionRoot, argsRootBuffer);
                    return result
                }, _teardown_after_call: function (converter, buffer, resultRoot, exceptionRoot, argsRootBuffer) {
                    this._release_args_root_buffer_from_method_call(converter, argsRootBuffer);
                    this._release_buffer_from_method_call(converter, buffer | 0);
                    if (resultRoot) resultRoot.release();
                    if (exceptionRoot) exceptionRoot.release()
                }, _get_method_description: function (method) {
                    if (!this._method_descriptions) this._method_descriptions = new Map;
                    var result = this._method_descriptions.get(method);
                    if (!result) result = "method#" + method;
                    return result
                }, _call_method_with_converted_args: function (method, this_arg, converter, buffer, is_result_marshaled, argsRootBuffer) {
                    var resultRoot = MONO.mono_wasm_new_root(),
                        exceptionRoot = MONO.mono_wasm_new_root();
                    resultRoot.value = this.invoke_method(method, this_arg, buffer, exceptionRoot.get_address());
                    return this._handle_exception_and_produce_result_for_call(converter, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled)
                }, bind_method: function (method, this_arg, args_marshal, friendly_name) {
                    this.bindings_lazy_init();
                    this_arg = this_arg | 0;
                    var converter = null;
                    if (typeof args_marshal === "string") converter = this._compile_converter_for_marshal_string(args_marshal);
                    var closure = { library_mono: MONO, binding_support: this, method: method, this_arg: this_arg };
                    var converterKey = "converter_" + converter.name;
                    if (converter) closure[converterKey] = converter;
                    var argumentNames = [];
                    var body = ["var resultRoot = library_mono.mono_wasm_new_root (), exceptionRoot = library_mono.mono_wasm_new_root ();", ""];
                    if (converter) {
                        body.push(`var argsRootBuffer = binding_support._get_args_root_buffer_for_method_call (${converterKey});`, `var scratchBuffer = binding_support._get_buffer_for_method_call (${converterKey});`, `var buffer = ${converterKey}.compiled_function (`, "    scratchBuffer, argsRootBuffer, method,");
                        for (var i = 0; i < converter.steps.length; i++) {
                            var argName = "arg" + i;
                            argumentNames.push(argName);
                            body.push("    " + argName + (i == converter.steps.length - 1 ? "" : ", "))
                        }
                        body.push(");")
                    } else {body.push("var argsRootBuffer = null, buffer = 0;")}
                    if (converter.is_result_definitely_unmarshaled) {body.push("var is_result_marshaled = false;")} else if (converter.is_result_possibly_unmarshaled) {body.push(`var is_result_marshaled = arguments.length !== ${converter.result_unmarshaled_if_argc};`)} else {body.push("var is_result_marshaled = true;")}
                    body.push("", "resultRoot.value = binding_support.invoke_method (method, this_arg, buffer, exceptionRoot.get_address ());", `binding_support._handle_exception_for_call (${converterKey}, buffer, resultRoot, exceptionRoot, argsRootBuffer);`, "", "var result = undefined;", "if (!is_result_marshaled) ", "    result = resultRoot.value;", "else if (resultRoot.value !== 0) {", "    var resultType = binding_support.mono_wasm_try_unbox_primitive_and_get_type (resultRoot.value, buffer);", "    switch (resultType) {", "    case 1:", "        result = Module.HEAP32[buffer / 4]; break;", "    case 25:", "        result = Module.HEAPU32[buffer / 4]; break;", "    case 24:", "        result = Module.HEAPF32[buffer / 4]; break;", "    case 2:", "        result = Module.HEAPF64[buffer / 8]; break;", "    case 8:", "        result = (Module.HEAP32[buffer / 4]) !== 0; break;", "    case 28:", "        result = String.fromCharCode(Module.HEAP32[buffer / 4]); break;", "    default:", "        result = binding_support._unbox_mono_obj_root_with_known_nonprimitive_type (resultRoot, resultType); break;", "    }", "}", "", `binding_support._teardown_after_call (${converterKey}, buffer, resultRoot, exceptionRoot, argsRootBuffer);`, "return result;");
                    bodyJs = body.join("\r\n");
                    if (friendly_name) {
                        var escapeRE = /[^A-Za-z0-9_]/g;
                        friendly_name = friendly_name.replace(escapeRE, "_")
                    }
                    var displayName = "managed_" + (friendly_name || method);
                    if (this_arg) displayName += "_with_this_" + this_arg;
                    return this._create_named_function(displayName, argumentNames, bodyJs, closure)
                }, resolve_method_fqn: function (fqn) {
                    this.bindings_lazy_init();
                    var assembly = fqn.substring(fqn.indexOf("[") + 1, fqn.indexOf("]")).trim();
                    fqn = fqn.substring(fqn.indexOf("]") + 1).trim();
                    var methodname = fqn.substring(fqn.indexOf(":") + 1);
                    fqn = fqn.substring(0, fqn.indexOf(":")).trim();
                    var namespace = "";
                    var classname = fqn;
                    if (fqn.indexOf(".") != -1) {
                        var idx = fqn.lastIndexOf(".");
                        namespace = fqn.substring(0, idx);
                        classname = fqn.substring(idx + 1)
                    }
                    if (!assembly.trim()) throw new Error("No assembly name specified");
                    if (!classname.trim()) throw new Error("No class name specified");
                    if (!methodname.trim()) throw new Error("No method name specified");
                    var asm = this.assembly_load(assembly);
                    if (!asm) throw new Error("Could not find assembly: " + assembly);
                    var klass = this.find_class(asm, namespace, classname);
                    if (!klass) throw new Error("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);
                    var method = this.find_method(klass, methodname, -1);
                    if (!method) throw new Error("Could not find method: " + methodname);
                    return method
                }, call_static_method: function (fqn, args, signature) {
                    this.bindings_lazy_init();
                    var method = this.resolve_method_fqn(fqn);
                    if (typeof signature === "undefined") signature = Module.mono_method_get_call_signature(method);
                    return this.call_method(method, null, signature, args)
                }, bind_static_method: function (fqn, signature) {
                    this.bindings_lazy_init();
                    var method = this.resolve_method_fqn(fqn);
                    if (typeof signature === "undefined") signature = Module.mono_method_get_call_signature(method);
                    return BINDING.bind_method(method, null, signature, fqn)
                }, bind_assembly_entry_point: function (assembly, signature) {
                    this.bindings_lazy_init();
                    var asm = this.assembly_load(assembly);
                    if (!asm) throw new Error("Could not find assembly: " + assembly);
                    var method = this.assembly_get_entry_point(asm);
                    if (!method) throw new Error("Could not find entry point for assembly: " + assembly);
                    if (typeof signature === "undefined") signature = Module.mono_method_get_call_signature(method);
                    return function () {
                        try {
                            var args = [...arguments];
                            if (args.length > 0 && Array.isArray(args[0])) args[0] = BINDING.js_array_to_mono_array(args[0], true, false);
                            let result = BINDING.call_method(method, null, signature, args);
                            return Promise.resolve(result)
                        } catch (error) {return Promise.reject(error)}
                    }
                }, call_assembly_entry_point: function (assembly, args, signature) {return this.bind_assembly_entry_point(assembly, signature)(...args)}, mono_wasm_get_jsobj_from_js_handle: function (js_handle) {
                    if (js_handle > 0) return this._cs_owned_objects_by_js_handle[js_handle];
                    return null
                }, mono_wasm_get_js_handle: function (js_obj) {
                    if (js_obj[BINDING.cs_owned_js_handle_symbol]) {return js_obj[BINDING.cs_owned_js_handle_symbol]}
                    var js_handle = this._js_handle_free_list.length ? this._js_handle_free_list.pop() : this._next_js_handle++;
                    this._cs_owned_objects_by_js_handle[js_handle] = js_obj;
                    js_obj[BINDING.cs_owned_js_handle_symbol] = js_handle;
                    return js_handle
                }, _mono_wasm_release_js_handle: function (js_handle) {
                    var obj = BINDING._cs_owned_objects_by_js_handle[js_handle];
                    if (typeof obj !== "undefined" && obj !== null) {
                        if (globalThis === obj) return obj;
                        if (typeof obj[BINDING.cs_owned_js_handle_symbol] !== "undefined") {obj[BINDING.cs_owned_js_handle_symbol] = undefined}
                        BINDING._cs_owned_objects_by_js_handle[js_handle] = undefined;
                        BINDING._js_handle_free_list.push(js_handle)
                    }
                    return obj
                }
            };

            function _mono_wasm_add_event_listener(objHandle, name, listener_gc_handle, optionsHandle) {
                var nameRoot = MONO.mono_wasm_new_root(name);
                try {
                    BINDING.bindings_lazy_init();
                    var sName = BINDING.conv_string(nameRoot.value);
                    var obj = BINDING.mono_wasm_get_jsobj_from_js_handle(objHandle);
                    if (!obj) throw new Error("ERR09: Invalid JS object handle for '" + sName + "'");
                    const prevent_timer_throttling = !BINDING.isChromium || obj.constructor.name !== "WebSocket" ? null : () => MONO.prevent_timer_throttling(0);
                    var listener = BINDING._wrap_delegate_gc_handle_as_function(listener_gc_handle, prevent_timer_throttling);
                    if (!listener) throw new Error("ERR10: Invalid listener gc_handle");
                    var options = optionsHandle ? BINDING.mono_wasm_get_jsobj_from_js_handle(optionsHandle) : null;
                    if (!BINDING._use_finalization_registry) {listener[BINDING.listener_registration_count_symbol] = listener[BINDING.listener_registration_count_symbol] ? listener[BINDING.listener_registration_count_symbol] + 1 : 1}
                    if (options) obj.addEventListener(sName, listener, options); else obj.addEventListener(sName, listener);
                    return 0
                } catch (exc) {return BINDING.js_string_to_mono_string(exc.message)} finally {nameRoot.release()}
            }

            function _mono_wasm_asm_loaded(assembly_name, assembly_ptr, assembly_len, pdb_ptr, pdb_len) {
                if (MONO.mono_wasm_runtime_is_ready !== true) return;
                const assembly_name_str = assembly_name !== 0 ? Module.UTF8ToString(assembly_name).concat(".dll") : "";
                const assembly_data = new Uint8Array(Module.HEAPU8.buffer, assembly_ptr, assembly_len);
                const assembly_b64 = MONO._base64Converter.toBase64StringImpl(assembly_data);
                let pdb_b64;
                if (pdb_ptr) {
                    const pdb_data = new Uint8Array(Module.HEAPU8.buffer, pdb_ptr, pdb_len);
                    pdb_b64 = MONO._base64Converter.toBase64StringImpl(pdb_data)
                }
                MONO.mono_wasm_raise_debug_event({ eventName: "AssemblyLoaded", assembly_name: assembly_name_str, assembly_b64: assembly_b64, pdb_b64: pdb_b64 })
            }

            function _mono_wasm_create_cs_owned_object(core_name, args, is_exception) {
                var argsRoot = MONO.mono_wasm_new_root(args),
                    nameRoot = MONO.mono_wasm_new_root(core_name);
                try {
                    BINDING.bindings_lazy_init();
                    var js_name = BINDING.conv_string(nameRoot.value);
                    if (!js_name) {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("Invalid name @" + nameRoot.value)
                    }
                    var coreObj = globalThis[js_name];
                    if (coreObj === null || typeof coreObj === "undefined") {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("JavaScript host object '" + js_name + "' not found.")
                    }
                    var js_args = BINDING._mono_array_root_to_js_array(argsRoot);
                    try {
                        var allocator = function (constructor, js_args) {
                            var argsList = new Array;
                            argsList[0] = constructor;
                            if (js_args) argsList = argsList.concat(js_args);
                            var tempCtor = constructor.bind.apply(constructor, argsList);
                            var js_obj = new tempCtor;
                            return js_obj
                        };
                        var js_obj = allocator(coreObj, js_args);
                        var js_handle = BINDING.mono_wasm_get_js_handle(js_obj);
                        return BINDING._js_to_mono_obj(false, js_handle)
                    } catch (e) {
                        var res = e.toString();
                        setValue(is_exception, 1, "i32");
                        if (res === null || res === undefined) res = "Error allocating object.";
                        return BINDING.js_string_to_mono_string(res)
                    }
                } finally {
                    argsRoot.release();
                    nameRoot.release()
                }
            }

            function _mono_wasm_fire_debugger_agent_message() {debugger}

            function _mono_wasm_get_by_index(js_handle, property_index, is_exception) {
                BINDING.bindings_lazy_init();
                var obj = BINDING.mono_wasm_get_jsobj_from_js_handle(js_handle);
                if (!obj) {
                    setValue(is_exception, 1, "i32");
                    return BINDING.js_string_to_mono_string("ERR03: Invalid JS object handle '" + js_handle + "' while getting [" + property_index + "]")
                }
                try {
                    var m = obj[property_index];
                    return BINDING._js_to_mono_obj(true, m)
                } catch (e) {
                    var res = e.toString();
                    setValue(is_exception, 1, "i32");
                    if (res === null || typeof res === "undefined") res = "unknown exception";
                    return BINDING.js_string_to_mono_string(res)
                }
            }

            function _mono_wasm_get_global_object(global_name, is_exception) {
                var nameRoot = MONO.mono_wasm_new_root(global_name);
                try {
                    BINDING.bindings_lazy_init();
                    var js_name = BINDING.conv_string(nameRoot.value);
                    var globalObj;
                    if (!js_name) {globalObj = globalThis} else {globalObj = globalThis[js_name]}
                    if (globalObj === null || typeof globalObj === undefined) {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("Global object '" + js_name + "' not found.")
                    }
                    return BINDING._js_to_mono_obj(true, globalObj)
                } finally {nameRoot.release()}
            }

            function _mono_wasm_get_object_property(js_handle, property_name, is_exception) {
                BINDING.bindings_lazy_init();
                var nameRoot = MONO.mono_wasm_new_root(property_name);
                try {
                    var js_name = BINDING.conv_string(nameRoot.value);
                    if (!js_name) {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("Invalid property name object '" + nameRoot.value + "'")
                    }
                    var obj = BINDING.mono_wasm_get_jsobj_from_js_handle(js_handle);
                    if (!obj) {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("ERR01: Invalid JS object handle '" + js_handle + "' while geting '" + js_name + "'")
                    }
                    var res;
                    try {
                        var m = obj[js_name];
                        return BINDING._js_to_mono_obj(true, m)
                    } catch (e) {
                        var res = e.toString();
                        setValue(is_exception, 1, "i32");
                        if (res === null || typeof res === "undefined") res = "unknown exception";
                        return BINDING.js_string_to_mono_string(res)
                    }
                } finally {nameRoot.release()}
            }

            var DOTNET = { conv_string: function (mono_obj) {return MONO.string_decoder.copy(mono_obj)} };

            function _mono_wasm_invoke_js_blazor(exceptionMessage, callInfo, arg0, arg1, arg2) {
                var mono_string = globalThis._mono_string_cached || (globalThis._mono_string_cached = Module.cwrap("mono_wasm_string_from_js", "number", ["string"]));
                try {
                    var blazorExports = globalThis.Blazor;
                    if (!blazorExports) {throw new Error("The blazor.webassembly.js library is not loaded.")}
                    return blazorExports._internal.invokeJSFromDotNet(callInfo, arg0, arg1, arg2)
                } catch (ex) {
                    var exceptionJsString = ex.message + "\n" + ex.stack;
                    var exceptionSystemString = mono_string(exceptionJsString);
                    setValue(exceptionMessage, exceptionSystemString, "i32");
                    return 0
                }
            }

            function _mono_wasm_invoke_js_marshalled(exceptionMessage, asyncHandleLongPtr, functionName, argsJson, treatResultAsVoid) {
                var mono_string = globalThis._mono_string_cached || (globalThis._mono_string_cached = Module.cwrap("mono_wasm_string_from_js", "number", ["string"]));
                try {
                    var u32Index = asyncHandleLongPtr >> 2;
                    var asyncHandleJsNumber = Module.HEAPU32[u32Index + 1] * 4294967296 + Module.HEAPU32[u32Index];
                    var funcNameJsString = DOTNET.conv_string(functionName);
                    var argsJsonJsString = argsJson && DOTNET.conv_string(argsJson);
                    var dotNetExports = globaThis.DotNet;
                    if (!dotNetExports) {throw new Error("The Microsoft.JSInterop.js library is not loaded.")}
                    if (asyncHandleJsNumber) {
                        dotNetExports.jsCallDispatcher.beginInvokeJSFromDotNet(asyncHandleJsNumber, funcNameJsString, argsJsonJsString, treatResultAsVoid);
                        return 0
                    } else {
                        var resultJson = dotNetExports.jsCallDispatcher.invokeJSFromDotNet(funcNameJsString, argsJsonJsString, treatResultAsVoid);
                        return resultJson === null ? 0 : mono_string(resultJson)
                    }
                } catch (ex) {
                    var exceptionJsString = ex.message + "\n" + ex.stack;
                    var exceptionSystemString = mono_string(exceptionJsString);
                    setValue(exceptionMessage, exceptionSystemString, "i32");
                    return 0
                }
            }

            function _mono_wasm_invoke_js_unmarshalled(exceptionMessage, funcName, arg0, arg1, arg2) {
                try {
                    var funcNameJsString = DOTNET.conv_string(funcName);
                    var dotNetExports = globalThis.DotNet;
                    if (!dotNetExports) {throw new Error("The Microsoft.JSInterop.js library is not loaded.")}
                    var funcInstance = dotNetExports.jsCallDispatcher.findJSFunction(funcNameJsString);
                    return funcInstance.call(null, arg0, arg1, arg2)
                } catch (ex) {
                    var exceptionJsString = ex.message + "\n" + ex.stack;
                    var mono_string = Module.cwrap("mono_wasm_string_from_js", "number", ["string"]);
                    var exceptionSystemString = mono_string(exceptionJsString);
                    setValue(exceptionMessage, exceptionSystemString, "i32");
                    return 0
                }
            }

            function _mono_wasm_invoke_js_with_args(js_handle, method_name, args, is_exception) {
                let argsRoot = MONO.mono_wasm_new_root(args),
                    nameRoot = MONO.mono_wasm_new_root(method_name);
                try {
                    BINDING.bindings_lazy_init();
                    var js_name = BINDING.conv_string(nameRoot.value);
                    if (!js_name || typeof js_name !== "string") {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("ERR12: Invalid method name object '" + nameRoot.value + "'")
                    }
                    var obj = BINDING.get_js_obj(js_handle);
                    if (!obj) {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("ERR13: Invalid JS object handle '" + js_handle + "' while invoking '" + js_name + "'")
                    }
                    var js_args = BINDING._mono_array_root_to_js_array(argsRoot);
                    var res;
                    try {
                        var m = obj[js_name];
                        if (typeof m === "undefined") throw new Error("Method: '" + js_name + "' not found for: '" + Object.prototype.toString.call(obj) + "'");
                        var res = m.apply(obj, js_args);
                        return BINDING._js_to_mono_obj(true, res)
                    } catch (e) {
                        var res = e.toString();
                        setValue(is_exception, 1, "i32");
                        if (res === null || res === undefined) res = "unknown exception";
                        return BINDING.js_string_to_mono_string(res)
                    }
                } finally {
                    argsRoot.release();
                    nameRoot.release()
                }
            }

            function _mono_wasm_release_cs_owned_object(js_handle) {
                BINDING.bindings_lazy_init();
                BINDING._mono_wasm_release_js_handle(js_handle)
            }

            function _mono_wasm_remove_event_listener(objHandle, name, listener_gc_handle, capture) {
                var nameRoot = MONO.mono_wasm_new_root(name);
                try {
                    BINDING.bindings_lazy_init();
                    var obj = BINDING.mono_wasm_get_jsobj_from_js_handle(objHandle);
                    if (!obj) throw new Error("ERR11: Invalid JS object handle");
                    var listener = BINDING._lookup_js_owned_object(listener_gc_handle);
                    if (!listener) return;
                    var sName = BINDING.conv_string(nameRoot.value);
                    obj.removeEventListener(sName, listener, !!capture);
                    if (!BINDING._use_finalization_registry) {
                        listener[BINDING.listener_registration_count_symbol]--;
                        if (listener[BINDING.listener_registration_count_symbol] === 0) {
                            BINDING._js_owned_object_table.delete(listener_gc_handle);
                            BINDING._release_js_owned_object_by_gc_handle(listener_gc_handle)
                        }
                    }
                    return 0
                } catch (exc) {return BINDING.js_string_to_mono_string(exc.message)} finally {nameRoot.release()}
            }

            function _mono_wasm_set_by_index(js_handle, property_index, value, is_exception) {
                var valueRoot = MONO.mono_wasm_new_root(value);
                try {
                    BINDING.bindings_lazy_init();
                    var obj = BINDING.mono_wasm_get_jsobj_from_js_handle(js_handle);
                    if (!obj) {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("ERR04: Invalid JS object handle '" + js_handle + "' while setting [" + property_index + "]")
                    }
                    var js_value = BINDING._unbox_mono_obj_root(valueRoot);
                    try {
                        obj[property_index] = js_value;
                        return true
                    } catch (e) {
                        var res = e.toString();
                        setValue(is_exception, 1, "i32");
                        if (res === null || typeof res === "undefined") res = "unknown exception";
                        return BINDING.js_string_to_mono_string(res)
                    }
                } finally {valueRoot.release()}
            }

            function _mono_wasm_set_object_property(js_handle, property_name, value, createIfNotExist, hasOwnProperty, is_exception) {
                var valueRoot = MONO.mono_wasm_new_root(value),
                    nameRoot = MONO.mono_wasm_new_root(property_name);
                try {
                    BINDING.bindings_lazy_init();
                    var property = BINDING.conv_string(nameRoot.value);
                    if (!property) {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("Invalid property name object '" + property_name + "'")
                    }
                    var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle(js_handle);
                    if (!js_obj) {
                        setValue(is_exception, 1, "i32");
                        return BINDING.js_string_to_mono_string("ERR02: Invalid JS object handle '" + js_handle + "' while setting '" + property + "'")
                    }
                    var result = false;
                    var js_value = BINDING._unbox_mono_obj_root(valueRoot);
                    if (createIfNotExist) {
                        js_obj[property] = js_value;
                        result = true
                    } else {
                        result = false;
                        if (!createIfNotExist) {if (!js_obj.hasOwnProperty(property)) return false}
                        if (hasOwnProperty === true) {
                            if (js_obj.hasOwnProperty(property)) {
                                js_obj[property] = js_value;
                                result = true
                            }
                        } else {
                            js_obj[property] = js_value;
                            result = true
                        }
                    }
                    return BINDING._box_js_bool(result)
                } finally {
                    nameRoot.release();
                    valueRoot.release()
                }
            }

            function _mono_wasm_typed_array_copy_from(js_handle, pinned_array, begin, end, bytes_per_element, is_exception) {
                BINDING.bindings_lazy_init();
                var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle(js_handle);
                if (!js_obj) {
                    setValue(is_exception, 1, "i32");
                    return BINDING.js_string_to_mono_string("ERR08: Invalid JS object handle '" + js_handle + "'")
                }
                var res = BINDING.typedarray_copy_from(js_obj, pinned_array, begin, end, bytes_per_element);
                return BINDING._js_to_mono_obj(false, res)
            }

            function _mono_wasm_typed_array_copy_to(js_handle, pinned_array, begin, end, bytes_per_element, is_exception) {
                BINDING.bindings_lazy_init();
                var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle(js_handle);
                if (!js_obj) {
                    setValue(is_exception, 1, "i32");
                    return BINDING.js_string_to_mono_string("ERR07: Invalid JS object handle '" + js_handle + "'")
                }
                var res = BINDING.typedarray_copy_to(js_obj, pinned_array, begin, end, bytes_per_element);
                return BINDING._js_to_mono_obj(false, res)
            }

            function _mono_wasm_typed_array_from(pinned_array, begin, end, bytes_per_element, type, is_exception) {
                BINDING.bindings_lazy_init();
                var res = BINDING.typed_array_from(pinned_array, begin, end, bytes_per_element, type);
                return BINDING._js_to_mono_obj(true, res)
            }

            function _mono_wasm_typed_array_to_array(js_handle, is_exception) {
                BINDING.bindings_lazy_init();
                var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle(js_handle);
                if (!js_obj) {
                    setValue(is_exception, 1, "i32");
                    return BINDING.js_string_to_mono_string("ERR06: Invalid JS object handle '" + js_handle + "'")
                }
                return BINDING.js_typed_array_to_array(js_obj, false)
            }

            function _schedule_background_exec() {
                ++MONO.pump_count;
                if (typeof globalThis.setTimeout === "function") {globalThis.setTimeout(MONO.pump_message, 0)}
            }

            function _setTempRet0(val) {setTempRet0(val)}

            function __isLeapYear(year) {return year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0)}

            function __arraySum(array, index) {
                var sum = 0;
                for (var i = 0; i <= index; sum += array[i++]) {}
                return sum
            }

            var __MONTH_DAYS_LEAP = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
            var __MONTH_DAYS_REGULAR = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

            function __addDays(date, days) {
                var newDate = new Date(date.getTime());
                while (days > 0) {
                    var leap = __isLeapYear(newDate.getFullYear());
                    var currentMonth = newDate.getMonth();
                    var daysInCurrentMonth = (leap ? __MONTH_DAYS_LEAP : __MONTH_DAYS_REGULAR)[currentMonth];
                    if (days > daysInCurrentMonth - newDate.getDate()) {
                        days -= daysInCurrentMonth - newDate.getDate() + 1;
                        newDate.setDate(1);
                        if (currentMonth < 11) {newDate.setMonth(currentMonth + 1)} else {
                            newDate.setMonth(0);
                            newDate.setFullYear(newDate.getFullYear() + 1)
                        }
                    } else {
                        newDate.setDate(newDate.getDate() + days);
                        return newDate
                    }
                }
                return newDate
            }

            function _strftime(s, maxsize, format, tm) {
                var tm_zone = HEAP32[tm + 40 >> 2];
                var date = { tm_sec: HEAP32[tm >> 2], tm_min: HEAP32[tm + 4 >> 2], tm_hour: HEAP32[tm + 8 >> 2], tm_mday: HEAP32[tm + 12 >> 2], tm_mon: HEAP32[tm + 16 >> 2], tm_year: HEAP32[tm + 20 >> 2], tm_wday: HEAP32[tm + 24 >> 2], tm_yday: HEAP32[tm + 28 >> 2], tm_isdst: HEAP32[tm + 32 >> 2], tm_gmtoff: HEAP32[tm + 36 >> 2], tm_zone: tm_zone ? UTF8ToString(tm_zone) : "" };
                var pattern = UTF8ToString(format);
                var EXPANSION_RULES_1 = { "%c": "%a %b %d %H:%M:%S %Y", "%D": "%m/%d/%y", "%F": "%Y-%m-%d", "%h": "%b", "%r": "%I:%M:%S %p", "%R": "%H:%M", "%T": "%H:%M:%S", "%x": "%m/%d/%y", "%X": "%H:%M:%S", "%Ec": "%c", "%EC": "%C", "%Ex": "%m/%d/%y", "%EX": "%H:%M:%S", "%Ey": "%y", "%EY": "%Y", "%Od": "%d", "%Oe": "%e", "%OH": "%H", "%OI": "%I", "%Om": "%m", "%OM": "%M", "%OS": "%S", "%Ou": "%u", "%OU": "%U", "%OV": "%V", "%Ow": "%w", "%OW": "%W", "%Oy": "%y" };
                for (var rule in EXPANSION_RULES_1) {pattern = pattern.replace(new RegExp(rule, "g"), EXPANSION_RULES_1[rule])}
                var WEEKDAYS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
                var MONTHS = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

                function leadingSomething(value, digits, character) {
                    var str = typeof value === "number" ? value.toString() : value || "";
                    while (str.length < digits) {str = character[0] + str}
                    return str
                }

                function leadingNulls(value, digits) {return leadingSomething(value, digits, "0")}

                function compareByDay(date1, date2) {
                    function sgn(value) {return value < 0 ? -1 : value > 0 ? 1 : 0}

                    var compare;
                    if ((compare = sgn(date1.getFullYear() - date2.getFullYear())) === 0) {if ((compare = sgn(date1.getMonth() - date2.getMonth())) === 0) {compare = sgn(date1.getDate() - date2.getDate())}}
                    return compare
                }

                function getFirstWeekStartDate(janFourth) {
                    switch (janFourth.getDay()) {
                        case 0:
                            return new Date(janFourth.getFullYear() - 1, 11, 29);
                        case 1:
                            return janFourth;
                        case 2:
                            return new Date(janFourth.getFullYear(), 0, 3);
                        case 3:
                            return new Date(janFourth.getFullYear(), 0, 2);
                        case 4:
                            return new Date(janFourth.getFullYear(), 0, 1);
                        case 5:
                            return new Date(janFourth.getFullYear() - 1, 11, 31);
                        case 6:
                            return new Date(janFourth.getFullYear() - 1, 11, 30)
                    }
                }

                function getWeekBasedYear(date) {
                    var thisDate = __addDays(new Date(date.tm_year + 1900, 0, 1), date.tm_yday);
                    var janFourthThisYear = new Date(thisDate.getFullYear(), 0, 4);
                    var janFourthNextYear = new Date(thisDate.getFullYear() + 1, 0, 4);
                    var firstWeekStartThisYear = getFirstWeekStartDate(janFourthThisYear);
                    var firstWeekStartNextYear = getFirstWeekStartDate(janFourthNextYear);
                    if (compareByDay(firstWeekStartThisYear, thisDate) <= 0) {if (compareByDay(firstWeekStartNextYear, thisDate) <= 0) {return thisDate.getFullYear() + 1} else {return thisDate.getFullYear()}} else {return thisDate.getFullYear() - 1}
                }

                var EXPANSION_RULES_2 = {
                    "%a": function (date) {return WEEKDAYS[date.tm_wday].substring(0, 3)}, "%A": function (date) {return WEEKDAYS[date.tm_wday]}, "%b": function (date) {return MONTHS[date.tm_mon].substring(0, 3)}, "%B": function (date) {return MONTHS[date.tm_mon]}, "%C": function (date) {
                        var year = date.tm_year + 1900;
                        return leadingNulls(year / 100 | 0, 2)
                    }, "%d": function (date) {return leadingNulls(date.tm_mday, 2)}, "%e": function (date) {return leadingSomething(date.tm_mday, 2, " ")}, "%g": function (date) {return getWeekBasedYear(date).toString().substring(2)}, "%G": function (date) {return getWeekBasedYear(date)}, "%H": function (date) {return leadingNulls(date.tm_hour, 2)}, "%I": function (date) {
                        var twelveHour = date.tm_hour;
                        if (twelveHour == 0) twelveHour = 12; else if (twelveHour > 12) twelveHour -= 12;
                        return leadingNulls(twelveHour, 2)
                    }, "%j": function (date) {return leadingNulls(date.tm_mday + __arraySum(__isLeapYear(date.tm_year + 1900) ? __MONTH_DAYS_LEAP : __MONTH_DAYS_REGULAR, date.tm_mon - 1), 3)}, "%m": function (date) {return leadingNulls(date.tm_mon + 1, 2)}, "%M": function (date) {return leadingNulls(date.tm_min, 2)}, "%n": function () {return "\n"}, "%p": function (date) {if (date.tm_hour >= 0 && date.tm_hour < 12) {return "AM"} else {return "PM"}}, "%S": function (date) {return leadingNulls(date.tm_sec, 2)}, "%t": function () {return "\t"}, "%u": function (date) {return date.tm_wday || 7}, "%U": function (date) {
                        var janFirst = new Date(date.tm_year + 1900, 0, 1);
                        var firstSunday = janFirst.getDay() === 0 ? janFirst : __addDays(janFirst, 7 - janFirst.getDay());
                        var endDate = new Date(date.tm_year + 1900, date.tm_mon, date.tm_mday);
                        if (compareByDay(firstSunday, endDate) < 0) {
                            var februaryFirstUntilEndMonth = __arraySum(__isLeapYear(endDate.getFullYear()) ? __MONTH_DAYS_LEAP : __MONTH_DAYS_REGULAR, endDate.getMonth() - 1) - 31;
                            var firstSundayUntilEndJanuary = 31 - firstSunday.getDate();
                            var days = firstSundayUntilEndJanuary + februaryFirstUntilEndMonth + endDate.getDate();
                            return leadingNulls(Math.ceil(days / 7), 2)
                        }
                        return compareByDay(firstSunday, janFirst) === 0 ? "01" : "00"
                    }, "%V": function (date) {
                        var janFourthThisYear = new Date(date.tm_year + 1900, 0, 4);
                        var janFourthNextYear = new Date(date.tm_year + 1901, 0, 4);
                        var firstWeekStartThisYear = getFirstWeekStartDate(janFourthThisYear);
                        var firstWeekStartNextYear = getFirstWeekStartDate(janFourthNextYear);
                        var endDate = __addDays(new Date(date.tm_year + 1900, 0, 1), date.tm_yday);
                        if (compareByDay(endDate, firstWeekStartThisYear) < 0) {return "53"}
                        if (compareByDay(firstWeekStartNextYear, endDate) <= 0) {return "01"}
                        var daysDifference;
                        if (firstWeekStartThisYear.getFullYear() < date.tm_year + 1900) {daysDifference = date.tm_yday + 32 - firstWeekStartThisYear.getDate()} else {daysDifference = date.tm_yday + 1 - firstWeekStartThisYear.getDate()}
                        return leadingNulls(Math.ceil(daysDifference / 7), 2)
                    }, "%w": function (date) {return date.tm_wday}, "%W": function (date) {
                        var janFirst = new Date(date.tm_year, 0, 1);
                        var firstMonday = janFirst.getDay() === 1 ? janFirst : __addDays(janFirst, janFirst.getDay() === 0 ? 1 : 7 - janFirst.getDay() + 1);
                        var endDate = new Date(date.tm_year + 1900, date.tm_mon, date.tm_mday);
                        if (compareByDay(firstMonday, endDate) < 0) {
                            var februaryFirstUntilEndMonth = __arraySum(__isLeapYear(endDate.getFullYear()) ? __MONTH_DAYS_LEAP : __MONTH_DAYS_REGULAR, endDate.getMonth() - 1) - 31;
                            var firstMondayUntilEndJanuary = 31 - firstMonday.getDate();
                            var days = firstMondayUntilEndJanuary + februaryFirstUntilEndMonth + endDate.getDate();
                            return leadingNulls(Math.ceil(days / 7), 2)
                        }
                        return compareByDay(firstMonday, janFirst) === 0 ? "01" : "00"
                    }, "%y": function (date) {return (date.tm_year + 1900).toString().substring(2)}, "%Y": function (date) {return date.tm_year + 1900}, "%z": function (date) {
                        var off = date.tm_gmtoff;
                        var ahead = off >= 0;
                        off = Math.abs(off) / 60;
                        off = off / 60 * 100 + off % 60;
                        return (ahead ? "+" : "-") + String("0000" + off).slice(-4)
                    }, "%Z": function (date) {return date.tm_zone}, "%%": function () {return "%"}
                };
                for (var rule in EXPANSION_RULES_2) {if (pattern.includes(rule)) {pattern = pattern.replace(new RegExp(rule, "g"), EXPANSION_RULES_2[rule](date))}}
                var bytes = intArrayFromString(pattern, false);
                if (bytes.length > maxsize) {return 0}
                writeArrayToMemory(bytes, s);
                return bytes.length - 1
            }

            function _time(ptr) {
                var ret = Date.now() / 1e3 | 0;
                if (ptr) {HEAP32[ptr >> 2] = ret}
                return ret
            }

            var FSNode = function (parent, name, mode, rdev) {
                if (!parent) {parent = this}
                this.parent = parent;
                this.mount = parent.mount;
                this.mounted = null;
                this.id = FS.nextInode++;
                this.name = name;
                this.mode = mode;
                this.node_ops = {};
                this.stream_ops = {};
                this.rdev = rdev
            };
            var readMode = 292 | 73;
            var writeMode = 146;
            Object.defineProperties(FSNode.prototype, { read: { get: function () {return (this.mode & readMode) === readMode}, set: function (val) {val ? this.mode |= readMode : this.mode &= ~readMode} }, write: { get: function () {return (this.mode & writeMode) === writeMode}, set: function (val) {val ? this.mode |= writeMode : this.mode &= ~writeMode} }, isFolder: { get: function () {return FS.isDir(this.mode)} }, isDevice: { get: function () {return FS.isChrdev(this.mode)} } });
            FS.FSNode = FSNode;
            FS.staticInit();
            Module["FS_createPath"] = FS.createPath;
            Module["FS_createDataFile"] = FS.createDataFile;
            Module["FS_createPath"] = FS.createPath;
            Module["FS_createDataFile"] = FS.createDataFile;
            Module["FS_createPreloadedFile"] = FS.createPreloadedFile;
            Module["FS_createLazyFile"] = FS.createLazyFile;
            Module["FS_createDevice"] = FS.createDevice;
            Module["FS_unlink"] = FS.unlink;
            MONO.export_functions(Module);
            BINDING.export_functions(Module);
            var ASSERTIONS = false;

            function intArrayFromString(stringy, dontAddNull, length) {
                var len = length > 0 ? length : lengthBytesUTF8(stringy) + 1;
                var u8array = new Array(len);
                var numBytesWritten = stringToUTF8Array(stringy, u8array, 0, u8array.length);
                if (dontAddNull) u8array.length = numBytesWritten;
                return u8array
            }

            var asmLibraryArg = {
                "__assert_fail": ___assert_fail, "__clock_gettime": ___clock_gettime, "__cxa_allocate_exception": ___cxa_allocate_exception, "__cxa_begin_catch": ___cxa_begin_catch, "__cxa_end_catch": ___cxa_end_catch, "__cxa_find_matching_catch_3": ___cxa_find_matching_catch_3, "__cxa_throw": ___cxa_throw, "__resumeException": ___resumeException, "__sys_access": ___sys_access, "__sys_chdir": ___sys_chdir, "__sys_chmod": ___sys_chmod, "__sys_connect": ___sys_connect, "__sys_fadvise64_64": ___sys_fadvise64_64, "__sys_fchmod": ___sys_fchmod, "__sys_fcntl64": ___sys_fcntl64, "__sys_fstat64": ___sys_fstat64, "__sys_fstatfs64": ___sys_fstatfs64, "__sys_ftruncate64": ___sys_ftruncate64, "__sys_getcwd": ___sys_getcwd, "__sys_getdents64": ___sys_getdents64, "__sys_getpid": ___sys_getpid, "__sys_getrusage": ___sys_getrusage, "__sys_ioctl": ___sys_ioctl, "__sys_link": ___sys_link, "__sys_lstat64": ___sys_lstat64, "__sys_madvise1": ___sys_madvise1, "__sys_mkdir": ___sys_mkdir, "__sys_mmap2": ___sys_mmap2, "__sys_msync": ___sys_msync, "__sys_munmap": ___sys_munmap, "__sys_open": ___sys_open, "__sys_readlink": ___sys_readlink, "__sys_recvfrom": ___sys_recvfrom, "__sys_rename": ___sys_rename, "__sys_rmdir": ___sys_rmdir, "__sys_sendto": ___sys_sendto, "__sys_setsockopt": ___sys_setsockopt, "__sys_shutdown": ___sys_shutdown, "__sys_socket": ___sys_socket, "__sys_stat64": ___sys_stat64, "__sys_symlink": ___sys_symlink, "__sys_unlink": ___sys_unlink, "__sys_utimensat": ___sys_utimensat, "abort": _abort, "clock_getres": _clock_getres, "clock_gettime": _clock_gettime, "compile_function": compile_function, "difftime": _difftime, "dotnet_browser_entropy": _dotnet_browser_entropy, "emscripten_asm_const_int": _emscripten_asm_const_int, "emscripten_get_heap_max": _emscripten_get_heap_max, "emscripten_memcpy_big": _emscripten_memcpy_big, "emscripten_resize_heap": _emscripten_resize_heap, "emscripten_thread_sleep": _emscripten_thread_sleep, "environ_get": _environ_get, "environ_sizes_get": _environ_sizes_get, "exit": _exit, "fd_close": _fd_close, "fd_fdstat_get": _fd_fdstat_get, "fd_pread": _fd_pread, "fd_pwrite": _fd_pwrite, "fd_read": _fd_read, "fd_seek": _fd_seek, "fd_sync": _fd_sync, "fd_write": _fd_write, "flock": _flock, "gai_strerror": _gai_strerror, "getTempRet0": _getTempRet0, "gettimeofday": _gettimeofday, "gmtime_r": _gmtime_r, "invoke_vi": invoke_vi, "llvm_eh_typeid_for": _llvm_eh_typeid_for, "localtime_r": _localtime_r, "mono_set_timeout": _mono_set_timeout, "mono_wasm_add_event_listener": _mono_wasm_add_event_listener, "mono_wasm_asm_loaded": _mono_wasm_asm_loaded, "mono_wasm_create_cs_owned_object": _mono_wasm_create_cs_owned_object, "mono_wasm_fire_debugger_agent_message": _mono_wasm_fire_debugger_agent_message, "mono_wasm_get_by_index": _mono_wasm_get_by_index, "mono_wasm_get_global_object": _mono_wasm_get_global_object, "mono_wasm_get_object_property": _mono_wasm_get_object_property, "mono_wasm_invoke_js_blazor": _mono_wasm_invoke_js_blazor, "mono_wasm_invoke_js_marshalled": _mono_wasm_invoke_js_marshalled, "mono_wasm_invoke_js_unmarshalled": _mono_wasm_invoke_js_unmarshalled, "mono_wasm_invoke_js_with_args": _mono_wasm_invoke_js_with_args, "mono_wasm_release_cs_owned_object": _mono_wasm_release_cs_owned_object, "mono_wasm_remove_event_listener": _mono_wasm_remove_event_listener, "mono_wasm_set_by_index": _mono_wasm_set_by_index, "mono_wasm_set_object_property": _mono_wasm_set_object_property, "mono_wasm_typed_array_copy_from": _mono_wasm_typed_array_copy_from, "mono_wasm_typed_array_copy_to": _mono_wasm_typed_array_copy_to, "mono_wasm_typed_array_from": _mono_wasm_typed_array_from, "mono_wasm_typed_array_to_array": _mono_wasm_typed_array_to_array, "schedule_background_exec": _schedule_background_exec, "setTempRet0": _setTempRet0, "strftime": _strftime, "time": _time, "tzset": _tzset
            };
            var asm = createWasm();
            var ___wasm_call_ctors = Module["___wasm_call_ctors"] = function () {return (___wasm_call_ctors = Module["___wasm_call_ctors"] = Module["asm"]["__wasm_call_ctors"]).apply(null, arguments)};
            var _mono_wasm_register_root = Module["_mono_wasm_register_root"] = function () {return (_mono_wasm_register_root = Module["_mono_wasm_register_root"] = Module["asm"]["mono_wasm_register_root"]).apply(null, arguments)};
            var _mono_wasm_deregister_root = Module["_mono_wasm_deregister_root"] = function () {return (_mono_wasm_deregister_root = Module["_mono_wasm_deregister_root"] = Module["asm"]["mono_wasm_deregister_root"]).apply(null, arguments)};
            var _mono_wasm_add_assembly = Module["_mono_wasm_add_assembly"] = function () {return (_mono_wasm_add_assembly = Module["_mono_wasm_add_assembly"] = Module["asm"]["mono_wasm_add_assembly"]).apply(null, arguments)};
            var _mono_wasm_add_satellite_assembly = Module["_mono_wasm_add_satellite_assembly"] = function () {return (_mono_wasm_add_satellite_assembly = Module["_mono_wasm_add_satellite_assembly"] = Module["asm"]["mono_wasm_add_satellite_assembly"]).apply(null, arguments)};
            var _mono_wasm_setenv = Module["_mono_wasm_setenv"] = function () {return (_mono_wasm_setenv = Module["_mono_wasm_setenv"] = Module["asm"]["mono_wasm_setenv"]).apply(null, arguments)};
            var _free = Module["_free"] = function () {return (_free = Module["_free"] = Module["asm"]["free"]).apply(null, arguments)};
            var _mono_wasm_register_bundled_satellite_assemblies = Module["_mono_wasm_register_bundled_satellite_assemblies"] = function () {return (_mono_wasm_register_bundled_satellite_assemblies = Module["_mono_wasm_register_bundled_satellite_assemblies"] = Module["asm"]["mono_wasm_register_bundled_satellite_assemblies"]).apply(null, arguments)};
            var _mono_wasm_load_runtime = Module["_mono_wasm_load_runtime"] = function () {return (_mono_wasm_load_runtime = Module["_mono_wasm_load_runtime"] = Module["asm"]["mono_wasm_load_runtime"]).apply(null, arguments)};
            var _malloc = Module["_malloc"] = function () {return (_malloc = Module["_malloc"] = Module["asm"]["malloc"]).apply(null, arguments)};
            var _mono_wasm_assembly_load = Module["_mono_wasm_assembly_load"] = function () {return (_mono_wasm_assembly_load = Module["_mono_wasm_assembly_load"] = Module["asm"]["mono_wasm_assembly_load"]).apply(null, arguments)};
            var _mono_wasm_find_corlib_class = Module["_mono_wasm_find_corlib_class"] = function () {return (_mono_wasm_find_corlib_class = Module["_mono_wasm_find_corlib_class"] = Module["asm"]["mono_wasm_find_corlib_class"]).apply(null, arguments)};
            var _mono_wasm_assembly_find_class = Module["_mono_wasm_assembly_find_class"] = function () {return (_mono_wasm_assembly_find_class = Module["_mono_wasm_assembly_find_class"] = Module["asm"]["mono_wasm_assembly_find_class"]).apply(null, arguments)};
            var _mono_wasm_assembly_find_method = Module["_mono_wasm_assembly_find_method"] = function () {return (_mono_wasm_assembly_find_method = Module["_mono_wasm_assembly_find_method"] = Module["asm"]["mono_wasm_assembly_find_method"]).apply(null, arguments)};
            var _mono_wasm_get_delegate_invoke = Module["_mono_wasm_get_delegate_invoke"] = function () {return (_mono_wasm_get_delegate_invoke = Module["_mono_wasm_get_delegate_invoke"] = Module["asm"]["mono_wasm_get_delegate_invoke"]).apply(null, arguments)};
            var _mono_wasm_box_primitive = Module["_mono_wasm_box_primitive"] = function () {return (_mono_wasm_box_primitive = Module["_mono_wasm_box_primitive"] = Module["asm"]["mono_wasm_box_primitive"]).apply(null, arguments)};
            var _mono_wasm_invoke_method = Module["_mono_wasm_invoke_method"] = function () {return (_mono_wasm_invoke_method = Module["_mono_wasm_invoke_method"] = Module["asm"]["mono_wasm_invoke_method"]).apply(null, arguments)};
            var _mono_wasm_assembly_get_entry_point = Module["_mono_wasm_assembly_get_entry_point"] = function () {return (_mono_wasm_assembly_get_entry_point = Module["_mono_wasm_assembly_get_entry_point"] = Module["asm"]["mono_wasm_assembly_get_entry_point"]).apply(null, arguments)};
            var _mono_wasm_string_get_utf8 = Module["_mono_wasm_string_get_utf8"] = function () {return (_mono_wasm_string_get_utf8 = Module["_mono_wasm_string_get_utf8"] = Module["asm"]["mono_wasm_string_get_utf8"]).apply(null, arguments)};
            var _mono_wasm_string_convert = Module["_mono_wasm_string_convert"] = function () {return (_mono_wasm_string_convert = Module["_mono_wasm_string_convert"] = Module["asm"]["mono_wasm_string_convert"]).apply(null, arguments)};
            var _mono_wasm_string_from_js = Module["_mono_wasm_string_from_js"] = function () {return (_mono_wasm_string_from_js = Module["_mono_wasm_string_from_js"] = Module["asm"]["mono_wasm_string_from_js"]).apply(null, arguments)};
            var _mono_wasm_string_from_utf16 = Module["_mono_wasm_string_from_utf16"] = function () {return (_mono_wasm_string_from_utf16 = Module["_mono_wasm_string_from_utf16"] = Module["asm"]["mono_wasm_string_from_utf16"]).apply(null, arguments)};
            var _mono_wasm_get_obj_type = Module["_mono_wasm_get_obj_type"] = function () {return (_mono_wasm_get_obj_type = Module["_mono_wasm_get_obj_type"] = Module["asm"]["mono_wasm_get_obj_type"]).apply(null, arguments)};
            var _mono_wasm_try_unbox_primitive_and_get_type = Module["_mono_wasm_try_unbox_primitive_and_get_type"] = function () {return (_mono_wasm_try_unbox_primitive_and_get_type = Module["_mono_wasm_try_unbox_primitive_and_get_type"] = Module["asm"]["mono_wasm_try_unbox_primitive_and_get_type"]).apply(null, arguments)};
            var _mono_unbox_int = Module["_mono_unbox_int"] = function () {return (_mono_unbox_int = Module["_mono_unbox_int"] = Module["asm"]["mono_unbox_int"]).apply(null, arguments)};
            var _mono_wasm_array_length = Module["_mono_wasm_array_length"] = function () {return (_mono_wasm_array_length = Module["_mono_wasm_array_length"] = Module["asm"]["mono_wasm_array_length"]).apply(null, arguments)};
            var _mono_wasm_array_get = Module["_mono_wasm_array_get"] = function () {return (_mono_wasm_array_get = Module["_mono_wasm_array_get"] = Module["asm"]["mono_wasm_array_get"]).apply(null, arguments)};
            var _mono_wasm_obj_array_new = Module["_mono_wasm_obj_array_new"] = function () {return (_mono_wasm_obj_array_new = Module["_mono_wasm_obj_array_new"] = Module["asm"]["mono_wasm_obj_array_new"]).apply(null, arguments)};
            var _mono_wasm_obj_array_set = Module["_mono_wasm_obj_array_set"] = function () {return (_mono_wasm_obj_array_set = Module["_mono_wasm_obj_array_set"] = Module["asm"]["mono_wasm_obj_array_set"]).apply(null, arguments)};
            var _mono_wasm_string_array_new = Module["_mono_wasm_string_array_new"] = function () {return (_mono_wasm_string_array_new = Module["_mono_wasm_string_array_new"] = Module["asm"]["mono_wasm_string_array_new"]).apply(null, arguments)};
            var _mono_wasm_exec_regression = Module["_mono_wasm_exec_regression"] = function () {return (_mono_wasm_exec_regression = Module["_mono_wasm_exec_regression"] = Module["asm"]["mono_wasm_exec_regression"]).apply(null, arguments)};
            var _mono_wasm_exit = Module["_mono_wasm_exit"] = function () {return (_mono_wasm_exit = Module["_mono_wasm_exit"] = Module["asm"]["mono_wasm_exit"]).apply(null, arguments)};
            var _mono_wasm_set_main_args = Module["_mono_wasm_set_main_args"] = function () {return (_mono_wasm_set_main_args = Module["_mono_wasm_set_main_args"] = Module["asm"]["mono_wasm_set_main_args"]).apply(null, arguments)};
            var _mono_wasm_strdup = Module["_mono_wasm_strdup"] = function () {return (_mono_wasm_strdup = Module["_mono_wasm_strdup"] = Module["asm"]["mono_wasm_strdup"]).apply(null, arguments)};
            var _mono_wasm_parse_runtime_options = Module["_mono_wasm_parse_runtime_options"] = function () {return (_mono_wasm_parse_runtime_options = Module["_mono_wasm_parse_runtime_options"] = Module["asm"]["mono_wasm_parse_runtime_options"]).apply(null, arguments)};
            var _mono_wasm_enable_on_demand_gc = Module["_mono_wasm_enable_on_demand_gc"] = function () {return (_mono_wasm_enable_on_demand_gc = Module["_mono_wasm_enable_on_demand_gc"] = Module["asm"]["mono_wasm_enable_on_demand_gc"]).apply(null, arguments)};
            var _mono_wasm_intern_string = Module["_mono_wasm_intern_string"] = function () {return (_mono_wasm_intern_string = Module["_mono_wasm_intern_string"] = Module["asm"]["mono_wasm_intern_string"]).apply(null, arguments)};
            var _mono_wasm_string_get_data = Module["_mono_wasm_string_get_data"] = function () {return (_mono_wasm_string_get_data = Module["_mono_wasm_string_get_data"] = Module["asm"]["mono_wasm_string_get_data"]).apply(null, arguments)};
            var _mono_wasm_typed_array_new = Module["_mono_wasm_typed_array_new"] = function () {return (_mono_wasm_typed_array_new = Module["_mono_wasm_typed_array_new"] = Module["asm"]["mono_wasm_typed_array_new"]).apply(null, arguments)};
            var _mono_wasm_unbox_enum = Module["_mono_wasm_unbox_enum"] = function () {return (_mono_wasm_unbox_enum = Module["_mono_wasm_unbox_enum"] = Module["asm"]["mono_wasm_unbox_enum"]).apply(null, arguments)};
            var _memset = Module["_memset"] = function () {return (_memset = Module["_memset"] = Module["asm"]["memset"]).apply(null, arguments)};
            var ___errno_location = Module["___errno_location"] = function () {return (___errno_location = Module["___errno_location"] = Module["asm"]["__errno_location"]).apply(null, arguments)};
            var _putchar = Module["_putchar"] = function () {return (_putchar = Module["_putchar"] = Module["asm"]["putchar"]).apply(null, arguments)};
            var _mono_background_exec = Module["_mono_background_exec"] = function () {return (_mono_background_exec = Module["_mono_background_exec"] = Module["asm"]["mono_background_exec"]).apply(null, arguments)};
            var _mono_wasm_get_icudt_name = Module["_mono_wasm_get_icudt_name"] = function () {return (_mono_wasm_get_icudt_name = Module["_mono_wasm_get_icudt_name"] = Module["asm"]["mono_wasm_get_icudt_name"]).apply(null, arguments)};
            var _mono_wasm_load_icu_data = Module["_mono_wasm_load_icu_data"] = function () {return (_mono_wasm_load_icu_data = Module["_mono_wasm_load_icu_data"] = Module["asm"]["mono_wasm_load_icu_data"]).apply(null, arguments)};
            var _mono_print_method_from_ip = Module["_mono_print_method_from_ip"] = function () {return (_mono_print_method_from_ip = Module["_mono_print_method_from_ip"] = Module["asm"]["mono_print_method_from_ip"]).apply(null, arguments)};
            var _mono_set_timeout_exec = Module["_mono_set_timeout_exec"] = function () {return (_mono_set_timeout_exec = Module["_mono_set_timeout_exec"] = Module["asm"]["mono_set_timeout_exec"]).apply(null, arguments)};
            var _ntohs = Module["_ntohs"] = function () {return (_ntohs = Module["_ntohs"] = Module["asm"]["ntohs"]).apply(null, arguments)};
            var _htons = Module["_htons"] = function () {return (_htons = Module["_htons"] = Module["asm"]["htons"]).apply(null, arguments)};
            var _mono_wasm_set_is_debugger_attached = Module["_mono_wasm_set_is_debugger_attached"] = function () {return (_mono_wasm_set_is_debugger_attached = Module["_mono_wasm_set_is_debugger_attached"] = Module["asm"]["mono_wasm_set_is_debugger_attached"]).apply(null, arguments)};
            var _mono_wasm_send_dbg_command_with_parms = Module["_mono_wasm_send_dbg_command_with_parms"] = function () {return (_mono_wasm_send_dbg_command_with_parms = Module["_mono_wasm_send_dbg_command_with_parms"] = Module["asm"]["mono_wasm_send_dbg_command_with_parms"]).apply(null, arguments)};
            var _mono_wasm_send_dbg_command = Module["_mono_wasm_send_dbg_command"] = function () {return (_mono_wasm_send_dbg_command = Module["_mono_wasm_send_dbg_command"] = Module["asm"]["mono_wasm_send_dbg_command"]).apply(null, arguments)};
            var _emscripten_main_thread_process_queued_calls = Module["_emscripten_main_thread_process_queued_calls"] = function () {return (_emscripten_main_thread_process_queued_calls = Module["_emscripten_main_thread_process_queued_calls"] = Module["asm"]["emscripten_main_thread_process_queued_calls"]).apply(null, arguments)};
            var _htonl = Module["_htonl"] = function () {return (_htonl = Module["_htonl"] = Module["asm"]["htonl"]).apply(null, arguments)};
            var __get_tzname = Module["__get_tzname"] = function () {return (__get_tzname = Module["__get_tzname"] = Module["asm"]["_get_tzname"]).apply(null, arguments)};
            var __get_daylight = Module["__get_daylight"] = function () {return (__get_daylight = Module["__get_daylight"] = Module["asm"]["_get_daylight"]).apply(null, arguments)};
            var __get_timezone = Module["__get_timezone"] = function () {return (__get_timezone = Module["__get_timezone"] = Module["asm"]["_get_timezone"]).apply(null, arguments)};
            var stackSave = Module["stackSave"] = function () {return (stackSave = Module["stackSave"] = Module["asm"]["stackSave"]).apply(null, arguments)};
            var stackRestore = Module["stackRestore"] = function () {return (stackRestore = Module["stackRestore"] = Module["asm"]["stackRestore"]).apply(null, arguments)};
            var stackAlloc = Module["stackAlloc"] = function () {return (stackAlloc = Module["stackAlloc"] = Module["asm"]["stackAlloc"]).apply(null, arguments)};
            var _setThrew = Module["_setThrew"] = function () {return (_setThrew = Module["_setThrew"] = Module["asm"]["setThrew"]).apply(null, arguments)};
            var ___cxa_can_catch = Module["___cxa_can_catch"] = function () {return (___cxa_can_catch = Module["___cxa_can_catch"] = Module["asm"]["__cxa_can_catch"]).apply(null, arguments)};
            var ___cxa_is_pointer_type = Module["___cxa_is_pointer_type"] = function () {return (___cxa_is_pointer_type = Module["___cxa_is_pointer_type"] = Module["asm"]["__cxa_is_pointer_type"]).apply(null, arguments)};
            var _memalign = Module["_memalign"] = function () {return (_memalign = Module["_memalign"] = Module["asm"]["memalign"]).apply(null, arguments)};
            var dynCall_iijj = Module["dynCall_iijj"] = function () {return (dynCall_iijj = Module["dynCall_iijj"] = Module["asm"]["dynCall_iijj"]).apply(null, arguments)};
            var dynCall_iij = Module["dynCall_iij"] = function () {return (dynCall_iij = Module["dynCall_iij"] = Module["asm"]["dynCall_iij"]).apply(null, arguments)};
            var dynCall_ji = Module["dynCall_ji"] = function () {return (dynCall_ji = Module["dynCall_ji"] = Module["asm"]["dynCall_ji"]).apply(null, arguments)};
            var dynCall_j = Module["dynCall_j"] = function () {return (dynCall_j = Module["dynCall_j"] = Module["asm"]["dynCall_j"]).apply(null, arguments)};
            var dynCall_iijji = Module["dynCall_iijji"] = function () {return (dynCall_iijji = Module["dynCall_iijji"] = Module["asm"]["dynCall_iijji"]).apply(null, arguments)};
            var dynCall_jiji = Module["dynCall_jiji"] = function () {return (dynCall_jiji = Module["dynCall_jiji"] = Module["asm"]["dynCall_jiji"]).apply(null, arguments)};
            var dynCall_iiji = Module["dynCall_iiji"] = function () {return (dynCall_iiji = Module["dynCall_iiji"] = Module["asm"]["dynCall_iiji"]).apply(null, arguments)};
            var dynCall_iijiiij = Module["dynCall_iijiiij"] = function () {return (dynCall_iijiiij = Module["dynCall_iijiiij"] = Module["asm"]["dynCall_iijiiij"]).apply(null, arguments)};
            var dynCall_iiiij = Module["dynCall_iiiij"] = function () {return (dynCall_iiiij = Module["dynCall_iiiij"] = Module["asm"]["dynCall_iiiij"]).apply(null, arguments)};
            var dynCall_jiiij = Module["dynCall_jiiij"] = function () {return (dynCall_jiiij = Module["dynCall_jiiij"] = Module["asm"]["dynCall_jiiij"]).apply(null, arguments)};
            var dynCall_viiijjii = Module["dynCall_viiijjii"] = function () {return (dynCall_viiijjii = Module["dynCall_viiijjii"] = Module["asm"]["dynCall_viiijjii"]).apply(null, arguments)};
            var dynCall_jd = Module["dynCall_jd"] = function () {return (dynCall_jd = Module["dynCall_jd"] = Module["asm"]["dynCall_jd"]).apply(null, arguments)};
            var dynCall_jf = Module["dynCall_jf"] = function () {return (dynCall_jf = Module["dynCall_jf"] = Module["asm"]["dynCall_jf"]).apply(null, arguments)};
            var dynCall_vijj = Module["dynCall_vijj"] = function () {return (dynCall_vijj = Module["dynCall_vijj"] = Module["asm"]["dynCall_vijj"]).apply(null, arguments)};
            var dynCall_iiijiiii = Module["dynCall_iiijiiii"] = function () {return (dynCall_iiijiiii = Module["dynCall_iiijiiii"] = Module["asm"]["dynCall_iiijiiii"]).apply(null, arguments)};
            var dynCall_vj = Module["dynCall_vj"] = function () {return (dynCall_vj = Module["dynCall_vj"] = Module["asm"]["dynCall_vj"]).apply(null, arguments)};
            var dynCall_jiiiii = Module["dynCall_jiiiii"] = function () {return (dynCall_jiiiii = Module["dynCall_jiiiii"] = Module["asm"]["dynCall_jiiiii"]).apply(null, arguments)};
            var dynCall_iji = Module["dynCall_iji"] = function () {return (dynCall_iji = Module["dynCall_iji"] = Module["asm"]["dynCall_iji"]).apply(null, arguments)};
            var dynCall_ij = Module["dynCall_ij"] = function () {return (dynCall_ij = Module["dynCall_ij"] = Module["asm"]["dynCall_ij"]).apply(null, arguments)};
            var dynCall_jij = Module["dynCall_jij"] = function () {return (dynCall_jij = Module["dynCall_jij"] = Module["asm"]["dynCall_jij"]).apply(null, arguments)};
            var dynCall_jijj = Module["dynCall_jijj"] = function () {return (dynCall_jijj = Module["dynCall_jijj"] = Module["asm"]["dynCall_jijj"]).apply(null, arguments)};
            var dynCall_iijjiii = Module["dynCall_iijjiii"] = function () {return (dynCall_iijjiii = Module["dynCall_iijjiii"] = Module["asm"]["dynCall_iijjiii"]).apply(null, arguments)};
            var dynCall_vijjjii = Module["dynCall_vijjjii"] = function () {return (dynCall_vijjjii = Module["dynCall_vijjjii"] = Module["asm"]["dynCall_vijjjii"]).apply(null, arguments)};
            var dynCall_iijii = Module["dynCall_iijii"] = function () {return (dynCall_iijii = Module["dynCall_iijii"] = Module["asm"]["dynCall_iijii"]).apply(null, arguments)};
            var dynCall_iijiii = Module["dynCall_iijiii"] = function () {return (dynCall_iijiii = Module["dynCall_iijiii"] = Module["asm"]["dynCall_iijiii"]).apply(null, arguments)};
            var dynCall_vijiiii = Module["dynCall_vijiiii"] = function () {return (dynCall_vijiiii = Module["dynCall_vijiiii"] = Module["asm"]["dynCall_vijiiii"]).apply(null, arguments)};
            var dynCall_iijiiii = Module["dynCall_iijiiii"] = function () {return (dynCall_iijiiii = Module["dynCall_iijiiii"] = Module["asm"]["dynCall_iijiiii"]).apply(null, arguments)};
            var dynCall_vij = Module["dynCall_vij"] = function () {return (dynCall_vij = Module["dynCall_vij"] = Module["asm"]["dynCall_vij"]).apply(null, arguments)};
            var dynCall_jii = Module["dynCall_jii"] = function () {return (dynCall_jii = Module["dynCall_jii"] = Module["asm"]["dynCall_jii"]).apply(null, arguments)};
            var dynCall_jiiiiiiiii = Module["dynCall_jiiiiiiiii"] = function () {return (dynCall_jiiiiiiiii = Module["dynCall_jiiiiiiiii"] = Module["asm"]["dynCall_jiiiiiiiii"]).apply(null, arguments)};
            var dynCall_jj = Module["dynCall_jj"] = function () {return (dynCall_jj = Module["dynCall_jj"] = Module["asm"]["dynCall_jj"]).apply(null, arguments)};
            var dynCall_iiijiiiii = Module["dynCall_iiijiiiii"] = function () {return (dynCall_iiijiiiii = Module["dynCall_iiijiiiii"] = Module["asm"]["dynCall_iiijiiiii"]).apply(null, arguments)};

            function invoke_vi(index, a1) {
                var sp = stackSave();
                try {wasmTable.get(index)(a1)} catch (e) {
                    stackRestore(sp);
                    if (e !== e + 0 && e !== "longjmp") throw e;
                    _setThrew(1, 0)
                }
            }

            Module["ccall"] = ccall;
            Module["cwrap"] = cwrap;
            Module["setValue"] = setValue;
            Module["getValue"] = getValue;
            Module["UTF8ArrayToString"] = UTF8ArrayToString;
            Module["UTF8ToString"] = UTF8ToString;
            Module["addRunDependency"] = addRunDependency;
            Module["removeRunDependency"] = removeRunDependency;
            Module["FS_createPath"] = FS.createPath;
            Module["FS_createDataFile"] = FS.createDataFile;
            Module["FS_createPreloadedFile"] = FS.createPreloadedFile;
            Module["FS_createLazyFile"] = FS.createLazyFile;
            Module["FS_createDevice"] = FS.createDevice;
            Module["FS_unlink"] = FS.unlink;
            Module["addFunction"] = addFunction;
            Module["MONO"] = MONO;
            Module["BINDING"] = BINDING;
            var calledRun;

            function ExitStatus(status) {
                this.name = "ExitStatus";
                this.message = "Program terminated with exit(" + status + ")";
                this.status = status
            }

            dependenciesFulfilled = function runCaller() {
                if (!calledRun) run();
                if (!calledRun) dependenciesFulfilled = runCaller
            };

            function run(args) {
                args = args || arguments_;
                if (runDependencies > 0) {return}
                preRun();
                if (runDependencies > 0) {return}

                function doRun() {
                    if (calledRun) return;
                    calledRun = true;
                    Module["calledRun"] = true;
                    if (ABORT) return;
                    initRuntime();
                    readyPromiseResolve(Module);
                    if (Module["onRuntimeInitialized"]) Module["onRuntimeInitialized"]();
                    postRun()
                }

                if (Module["setStatus"]) {
                    Module["setStatus"]("Running...");
                    setTimeout(function () {
                        setTimeout(function () {Module["setStatus"]("")}, 1);
                        doRun()
                    }, 1)
                } else {doRun()}
            }

            Module["run"] = run;

            function exit(status, implicit) {
                EXITSTATUS = status;
                if (implicit && keepRuntimeAlive() && status === 0) {return}
                if (keepRuntimeAlive()) {} else {
                    exitRuntime();
                    if (Module["onExit"]) Module["onExit"](status);
                    ABORT = true
                }
                quit_(status, new ExitStatus(status))
            }

            if (Module["preInit"]) {
                if (typeof Module["preInit"] == "function") Module["preInit"] = [Module["preInit"]];
                while (Module["preInit"].length > 0) {Module["preInit"].pop()()}
            }
            run();


            return Module.ready
        }
    );
})();
if (typeof exports === 'object' && typeof module === 'object')
    module.exports = Module;
else if (typeof define === 'function' && define['amd'])
    define([], function () { return Module; });
else if (typeof exports === 'object')
    exports["Module"] = Module;
