import { render, act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Backend } from "backend";
import { Stress } from "stress";

beforeEach(() => {
    Backend.StartStress = jest.fn();
    Backend.StopStress = jest.fn();
    Backend.IsStressing = jest.fn();
});

test("stress is not running initially", () => {
    render(<Stress power={0}/>);
    expect(Backend.StartStress).not.toBeCalled();
});

test("get stress power returns value specified in props", async () => {
    render(<Stress power={666}/>);
    expect(Backend.GetStressPower()).toEqual(666);
});

test("stress iteration time is written to screen", async () => {
    render(<Stress power={0}/>);
    await act(() => Backend.OnStressComplete.broadcast(13));
    expect(screen.getByText(/Stressed over 13ms/));
});

test("button click stops stress when stress is running", async () => {
    Backend.IsStressing = () => true;
    render(<Stress power={0}/>);
    await userEvent.click(screen.getByRole("button"));
    expect(Backend.StopStress).toBeCalled();
});

test("button click starts stress when stress is not running", async () => {
    Backend.IsStressing = () => false;
    render(<Stress power={0}/>);
    await userEvent.click(screen.getByRole("button"));
    expect(Backend.StartStress).toBeCalled();
});
