export type ApiSuccess<Result> = {
    json: 
        { result: Result} |
        { results: Result[] }
}
export type ApiFailure = {
    json: {
        errors: Record<string, string[]>,
        type: string,
        title: string,
        status: number, 
        traceId: string
    }
}

export default function webRequest<Result, Payload>(
    url: string, 
    data?: Payload, 
    opts?: object & { 
        blob?: Blob,
        method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE' | 'OPTIONS',
        locale?: string,
        includes: string[]
    }
): Promise<ApiSuccess<Result> | ApiFailure>