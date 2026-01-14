/**
 * Converts the given set of fields in to a query string.
 * @param {any} fields
 * @returns
 */
export default function queryString(fields: Record<string, string>): string {
    if (!fields) return "";
    return "?" + new URLSearchParams(fields).toString();
}