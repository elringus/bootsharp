import { Computer as Backend, PrimeUI as UI } from "backend";
import { beforeEach, test, expect, vi } from "vitest";
import { render, act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Computer from "../src/computer";

beforeEach(() => {
    Backend.startComputing = vi.fn();
    Backend.stopComputing = vi.fn();
    Backend.isComputing = vi.fn(() => false);
});

test("not computing initially", () => {
    render(<Computer options={{ complexity: 0, multithreading: false }} resultLimit={0}/>);
    expect(Backend.startComputing).not.toHaveBeenCalled();
});

test("get options returns values specified in props", async () => {
    render(<Computer options={{ complexity: 666, multithreading: true }} resultLimit={0}/>);
    expect(UI.$getOptions()).toStrictEqual({ complexity: 666, multithreading: true });
});

test("compute time is written to screen", async () => {
    render(<Computer options={{ complexity: 0, multithreading: false }} resultLimit={99}/>);
    act(() => UI.onComplete.broadcast(13));
    expect(screen.getByText(/Computed in 13ms/));
});

test("button click stops computing when running", async () => {
    Backend.isComputing = () => true;
    render(<Computer options={{ complexity: 0, multithreading: false }} resultLimit={0}/>);
    await userEvent.click(screen.getByRole("button"));
    expect(Backend.stopComputing).toHaveBeenCalled();
});

test("button click starts computing when not running", async () => {
    Backend.isComputing = () => false;
    render(<Computer options={{ complexity: 0, multithreading: false }} resultLimit={0}/>);
    await userEvent.click(screen.getByRole("button"));
    expect(Backend.startComputing).toHaveBeenCalled();
});
