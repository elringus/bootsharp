import { render, act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Backend } from "backend";
import { Stress } from "stress";

beforeEach(() => {
    Backend.StartStress = jest.fn();
    Backend.StopStress = jest.fn();
    Backend.IsStressing = jest.fn();
});

test("stress starts on render", () => {
    render(<Stress power={0}/>);
    expect(Backend.StartStress).toBeCalledTimes(1);
});

test("get stress power returns value specified in props", async () => {
    render(<Stress power={666}/>);
    expect(await Backend.GetStressPower()).toEqual(666);
});

test("stress iteration time is written to screen", async () => {
    render(<Stress power={0}/>);
    await act(() => Backend.OnStressIteration.broadcast(13));
    expect(screen.getByText(/Stressed over 13ms/));
});

test("button click stops stress when stress is running", async () => {
    Backend.IsStressing = async () => true;
    render(<Stress power={0}/>);
    await userEvent.click(screen.getByRole("button"));
    expect(Backend.StopStress).toBeCalledTimes(1);
});

test("button click starts stress when stress is not running", async () => {
    Backend.IsStressing = async () => false;
    render(<Stress power={0}/>);
    await userEvent.click(screen.getByRole("button"));
    expect(Backend.StartStress).toBeCalledTimes(2);
});
