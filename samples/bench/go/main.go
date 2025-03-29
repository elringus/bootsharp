package main

import (
  "encoding/json"
  "syscall/js"
)

type Data struct {
  Info     string   `json:"Info"`
  Ok       bool     `json:"Ok"`
  Revision int      `json:"Revision"`
  Messages []string `json:"Messages"`
}

func main() {
  js.Global().Set("echoNumber", js.FuncOf(echoNumber))
  js.Global().Set("echoStruct", js.FuncOf(echoStruct))
  js.Global().Set("fi", js.FuncOf(fi))
  <-make(chan struct{})
}

func echoNumber(_ js.Value, _ []js.Value) any {
  return js.Global().Call("getNumber").Int()
}

func echoStruct(_ js.Value, _ []js.Value) any {
  jsonStr := js.Global().Call("getStruct").String()
  var data Data
  if err := json.Unmarshal([]byte(jsonStr), &data); err != nil {
    return "Error: " + err.Error()
  }
  resultJson, _ := json.Marshal(data)
  return string(resultJson)
}

func fi(_ js.Value, args []js.Value) any {
  n := args[0].Int()
  return fibonacci(n)
}

func fibonacci(n int) int {
  if n <= 1 {
    return n
  }
  return fibonacci(n-1) + fibonacci(n-2)
}
