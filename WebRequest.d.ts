export default function webRequest(
    origUrl: string,
    data?: FormData | unknown,
    opts?: object & {
        blob?: unknown;
        method?: string;
        locale?: unknown;
    }
): Promise<unknown>;
