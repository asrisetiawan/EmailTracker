using Ardalis.Result;
using EmailTracker.Models;

namespace EmailTracker.Helpers
{

    public static class ArdalisResultExtensions
    {
        public static ApiResult<T> ToApiResult<T>(this Result<T> result)
        {
            return result.Status switch
            {
                ResultStatus.Ok or ResultStatus.Created =>
                    new ApiResult<T>
                    {
                        Success = true,
                        StatusCode = result.Status == ResultStatus.Created ? 201 : 200,
                        Data = result.Value
                    },

                ResultStatus.Conflict =>
                    new ApiResult<T>
                    {
                        Success = false,
                        StatusCode = 409,
                        Message = result.Errors.FirstOrDefault(),
                        Errors = new[]
                        {
                        new ApiError
                        {
                            Code = "conflict",
                            Message = result.Errors.FirstOrDefault() ?? "Conflict"
                        }
                        }
                    },

                ResultStatus.Invalid =>
                    new ApiResult<T>
                    {
                        Success = false,
                        StatusCode = 400,
                        Errors = result.ValidationErrors.Select(e =>
                            new ApiError
                            {
                                Code = e.Identifier ?? "validation_error",
                                Message = e.ErrorMessage
                            })
                    },

                ResultStatus.NotFound =>
                    new ApiResult<T>
                    {
                        Success = false,
                        StatusCode = 404,
                        Message = "Resource not found"
                    },

                ResultStatus.Unauthorized =>
                   new ApiResult<T>
                   {
                       Success = false,
                       StatusCode = 401,
                       Message = result.Errors.FirstOrDefault(),
                       Errors = new[]
                       {
                            new ApiError
                            {
                                Code = "Unauthorized",
                                Message = result.Errors.FirstOrDefault() ?? "Unauthorized"
                            }
                       }
                   },
                //ResultStatus.Unauthorized =>
                //    new ApiResult<T>
                //    {
                //        Success = false,
                //        StatusCode = 401,
                //        Message = "Unauthorized"
                //    },

                ResultStatus.Forbidden =>
                    new ApiResult<T>
                    {
                        Success = false,
                        StatusCode = 403,
                        Message = "Forbidden"
                    },

                _ =>
                    new ApiResult<T>
                    {
                        Success = false,
                        StatusCode = 500,
                        Message = result.Errors.FirstOrDefault() ?? "Internal server error"
                    }
            };
        }
    }
}
