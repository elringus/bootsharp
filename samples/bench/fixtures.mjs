/**
 * @typedef {Object} Data
 * @property {string} info
 * @property {boolean} ok
 * @property {number} revision
 * @property {string[]} messages
 */

/** @returns {number} */
export const getNumber = () => 42;

/** @returns {Data} */
export const getStruct = () => ({
    info: "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
    ok: true,
    revision: -112,
    messages: ["foo", "bar", "baz", "nya", "far"]
});
