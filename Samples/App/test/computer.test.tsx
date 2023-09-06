import { render, act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Backend, Frontend } from "backend";
import Computer from "computer";

beforeEach(() => {
    Backend.startStress = jest.fn();
    Backend.stopStress = jest.fn();
    Backend.isStressing = jest.fn();
});

test("stress is not running initially", () => {
    render(<Computer complexity={0}/>);
    expect(Backend.startStress).not.toBeCalled();
});

test("get stress power returns value specified in props", async () => {
    render(<Computer complexity={666}/>);
    expect(Frontend.getStressPower()).toEqual(666);
});

test("stress iteration time is written to screen", async () => {
    render(<Computer complexity={0}/>);
    await act(() => Frontend.onStressComplete.broadcast(13));
    expect(screen.getByText(/Stressed over 13ms/));
});

test("button click stops stress when stress is running", async () => {
    Backend.isStressing = () => true;
    render(<Computer complexity={0}/>);
    await userEvent.click(screen.getByRole("button"));
    expect(Backend.stopStress).toBeCalled();
});

test("button click starts stress when stress is not running", async () => {
    Backend.isStressing = () => false;
    render(<Computer complexity={0}/>);
    await userEvent.click(screen.getByRole("button"));
    expect(Backend.startStress).toBeCalled();
});
