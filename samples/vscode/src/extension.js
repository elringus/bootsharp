import * as vscode from "vscode";
import bootsharp, { Program } from "../../Minimal/cs/bin/bootsharp/index.mjs";

export async function activate(context) {
    Program.getFrontendName = () => "VS Code";
    try { await bootsharp.boot(); }
    catch (e) { vscode.window.showErrorMessage(e.message); }
    const command = vscode.commands.registerCommand("bootsharp.hello", greet);
    context.subscriptions.push(command);
}

export function deactivate() {
    bootsharp.exit();
}

function greet() {
    const message = `Welcome, ${Program.getBackendName()}! Enjoy your VS Code extension space.`;
    vscode.window.showInformationMessage(message);
}
