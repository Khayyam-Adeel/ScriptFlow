// Mirrors the problem+json body written by ScriptFlow.API's ExceptionHandlingMiddleware.
export interface ProblemDetails {
  title: string;
  status: number;
  detail: string;
  traceId: string;
}
