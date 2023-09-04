import * as vscode from "vscode";
import bootsharp, { Backend, Frontend } from "../../Minimal/cs/bin/bootsharp/bootsharp";

export async function activate(context) {
    Frontend.getName = () => "VS Code";
    try { await bootsharp.boot(); }
    catch (e) { vscode.window.showErrorMessage(e.message); }
    const command = vscode.commands.registerCommand("bootsharp.hello", greet);
    context.subscriptions.push(command);
}

export function deactivate() {
    bootsharp.exit();
}

function greet() {
    const message = `Welcome, ${Backend.getName()}! Enjoy your VS Code extension space.`;
    vscode.window.showInformationMessage(message);
}
