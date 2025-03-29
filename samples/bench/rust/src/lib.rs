use serde::{Deserialize, Serialize};
use wasm_bindgen::prelude::*;

#[derive(Serialize, Deserialize)]
pub struct Data {
    pub info: String,
    pub ok: bool,
    pub revision: i32,
    pub messages: Vec<String>,
}

#[wasm_bindgen]
extern "C" {
    #[wasm_bindgen(js_name = getNumber)]
    fn get_number() -> i32;
    #[wasm_bindgen(js_name = getStruct)]
    fn get_struct() -> String;
}

#[wasm_bindgen(js_name = echoNumber)]
pub fn echo_number() -> i32 {
    get_number()
}

#[wasm_bindgen(js_name = echoStruct)]
pub fn echo_struct() -> String {
    let json = get_struct();
    let data: Data = serde_json::from_str(&json).unwrap();
    serde_json::to_string(&data).unwrap()
}

#[wasm_bindgen]
pub fn fi(n: i32) -> i32 {
    if n <= 1 {
        return n;
    }
    fi(n - 1) + fi(n - 2)
}
