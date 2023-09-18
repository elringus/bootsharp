import { Computer as Backend, PrimeComputerUI as UI } from "backend";
import { beforeEach, test, expect, mock } from "bun:test";
import { render, act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Computer from "../src/computer";

beforeEach(() => {
    Backend.startComputing = mock(() => {});
    Backend.stopComputing = mock(() => {});
    Backend.isComputing = mock(() => false);
});

test("not computing initially", () => {
    render(<Computer complexity={0} resultLimit={0}/>);
    expect(Backend.startComputing).not.toHaveBeenCalled();
});

test("get complexity returns value specified in props", async () => {
    render(<Computer complexity={666} resultLimit={0}/>);
    expect(UI.getComplexity()).toEqual(666);
});

test("compute time is written to screen", async () => {
    render(<Computer complexity={0} resultLimit={99}/>);
    act(() => UI.onComplete.broadcast(13));
    expect(screen.getAllByText(/Computed in 13ms./)).not.toBeEmpty();
});

test("button click stops computing when running", async () => {
    Backend.isComputing = () => true;
    render(<Computer complexity={0} resultLimit={0}/>);
    await userEvent.click(screen.getAllByRole("button")[0]);
    expect(Backend.stopComputing).toHaveBeenCalled();
});

test("button click starts computing when not running", async () => {
    Backend.isComputing = () => false;
    render(<Computer complexity={0} resultLimit={0}/>);
    await userEvent.click(screen.getAllByRole("button")[0]);
    expect(Backend.startComputing).toHaveBeenCalled();
});
