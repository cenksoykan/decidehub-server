using System;
using System.Collections.Generic;
using System.Linq;
using Decidehub.Web.ViewModels.Api;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Decidehub.Web.Helpers
{
    public static class Errors
    {
        public static List<ErrorViewModel> GetErrorList(ModelStateDictionary modelState)
        {
            return modelState.SelectMany(state
                => state.Value.Errors.Select(modelError => new ErrorViewModel
                {
                    Title = state.Key, Description = modelError.ErrorMessage
                })).ToList();
        }

        public static List<ErrorViewModel> GetSingleErrorList(string title, string desc, Exception ex = null)
        {
            var list = new List<ErrorViewModel>
            {
                new ErrorViewModel {Title = title, Description = desc, Detail = ex?.GetAllMessages()}
            };


            return list;
        }

        private static string GetAllMessages(this Exception exp)
        {
            var message = string.Empty;
            var innerException = exp;

            do
            {
                message += string.IsNullOrEmpty(innerException.Message)
                    ? string.Empty
                    : innerException.Message;
                innerException = innerException.InnerException;
            } while (innerException != null);

            return message;
        }
    }
}