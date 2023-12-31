import * as vscode from "vscode";
import bootsharp, { Global } from "../../Minimal/cs/bin/bootsharp/bootsharp.mjs";

export async function activate(context) {
    Global.getFrontendName = () => "VS Code";
    try { await bootsharp.boot(); }
    catch (e) { vscode.window.showErrorMessage(e.message); }
    const command = vscode.commands.registerCommand("bootsharp.hello", greet);
    context.subscriptions.push(command);
}

export function deactivate() {
    bootsharp.exit();
}

function greet() {
    const message = `Welcome, ${Global.getBackendName()}! Enjoy your VS Code extension space.`;
    vscode.window.showInformationMessage(message);
}
