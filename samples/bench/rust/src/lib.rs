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
    fn get_struct() -> JsValue;
}

#[wasm_bindgen(js_name = echoNumber)]
pub fn echo_number() -> i32 {
    get_number()
}

#[wasm_bindgen(js_name = echoStruct)]
pub fn echo_struct() -> Result<JsValue, JsValue> {
    let js_data = get_struct();
    let data: Data = serde_wasm_bindgen::from_value(js_data)?;
    Ok(serde_wasm_bindgen::to_value(&data)?)
}

#[wasm_bindgen]
pub fn fi(n: i32) -> i32 {
    if n <= 1 {
        return n;
    }
    fi(n - 1) + fi(n - 2)
}
