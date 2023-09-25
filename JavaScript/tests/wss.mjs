import { WebSocketServer } from "ws";
import { parentPort } from "node:worker_threads";

const wss = new WebSocketServer({ port: 8080 });
wss.on("connection", socket => socket.on("message", socket.send));
parentPort.postMessage("ready");
