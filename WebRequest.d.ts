interface WebRequestResponse<Result> extends Omit<Response, "json"> {
    json: Result;
    notificationKey: unknown;
}

export default function webRequest<Data, Result>(
    origUrl: string,
    data?: Data,
    opts?: object & {
        blob?: unknown;
        method?: string;
        locale?: unknown;
    }
): Promise<WebRequestResponse<Result>>;
