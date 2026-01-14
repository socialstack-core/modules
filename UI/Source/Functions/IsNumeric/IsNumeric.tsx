export default function (n: any) {
    if (typeof n == 'string') {
        return !isNaN(parseFloat(n as string));
    } else if (typeof n == 'number') {
        return isFinite(n as number);
    }
    return false;
}