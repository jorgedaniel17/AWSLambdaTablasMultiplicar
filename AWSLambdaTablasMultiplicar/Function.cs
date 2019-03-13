using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Alexa.NET.Response.Directive;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSLambdaTablasMultiplicar
{
    public class Function
    {
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            var response = new SkillResponse();

            try
            {
                if (input.Request.GetType().Equals(typeof(LaunchRequest)))
                {
                    response = this.MakeSkillResponse($"{ResourceMessage.Welcome} {ResourceMessage.Help}", false);
                }
                else
                {
                    if (input.Request.Type.Equals(AlexaConstants.IntentRequest) && !((IntentRequest)input.Request).DialogState.Equals("COMPLETED"))
                    {
                        response = this.GetIntentRequest(input, context);
                    }
                    else if (input.Request.Type.Equals(AlexaConstants.SessionEndedRequest))
                    {
                        Log(context, $"session end: ");
                    }
                }

                Log(context, JsonConvert.SerializeObject(response));
                return response;
            }
            catch (Exception ex)
            {
                Log(context, $"error :" + ex.Message);
            }

            ((PlainTextOutputSpeech)response.Response.OutputSpeech).Text = ResourceMessage.Help;
            return response;
        }

        /// <summary>
        /// Process the Intent requests and return the speeech output
        /// </summary>
        /// <param name="input"></param>
        /// <returns>IOutputSpeech innerResponse</returns>
        private SkillResponse GetIntentRequest(SkillRequest input, ILambdaContext context)
        {
            var intentRequest = (IntentRequest)input.Request;
            
            switch (intentRequest.Intent.Name)
            {
                case AlexaConstants.TablasMultiplicarIntent:
                case AlexaConstants.StartOverIntent:
                    return this.AskQuestion(input);
                case AlexaConstants.CancelIntent:
                case AlexaConstants.StopIntent:
                    return this.MakeSkillResponse(ResourceMessage.ExitSkill, true);
                case AlexaConstants.HelpIntent:
                    return this.MakeSkillResponse(ResourceMessage.Help, false);
                default:
                    return this.MakeSkillResponse(ResourceMessage.Welcome, false);
            }
        }

        /// <summary>
        /// Builds up the details required to ask a question and stores them in the
        /// sessionAtrtibutes array
        /// </summary>
        /// <param name="input"></param>
        /// <param name="innerReponse"></param>
        /// <returns>void</returns>
        private SkillResponse AskQuestion(SkillRequest input)
        {
            var request = (IntentRequest)input.Request;

            if (request?.Intent?.Slots == null || !request.Intent.Slots.Any())
            {
                return MakeSkillResponse($"No se envio un valor", false, this.GetAttributes(input));
            }

            int.TryParse(request.Intent.Slots[AlexaConstants.SlotName]?.Value, out int number);
            if (number == 0)
            {
                number = this.GetNumberOfString((request.Intent.Slots[AlexaConstants.SlotName]?.Value ?? string.Empty).Trim().ToLower());
            }

            if (input?.Session?.Attributes == null || !input.Session.Attributes.Any())
            {
                if (number < 1 || number > 11) { return MakeSkillResponse($"La tabla solicitada esta fuera de rango {number}"); }

                var requestattributes = new AttributesDto
                {
                    CurrentQuestionCounter = 0,
                    PreviousAnswer = number * 1,
                    Number01 = number,
                    Number02 = 1
                };
                
                return MakeSkillResponse($"{this.GetQuestion(number, 1)}", false, requestattributes);
            }

            var responseAttributes = input.Session.Attributes.First().Value.ToString();
            var values = this.GetAttributes(input);
            
            if (values != null && values.CurrentQuestionCounter > 1)
            {
                values.CurrentQuestionCounter = 0;
                var message = $"Excediste los intentos, la respuesta es {values.PreviousAnswer}, intentalo nuevamente {this.GetQuestion(values.Number01, values.Number02)}";
                return MakeSkillResponse(message, false, values);
            }

            var number01 = values.Number01;
            var number02 = values.Number02;
            var correctAnswer = values.PreviousAnswer;
            string speechOutput;
            if (values == null) { values = new AttributesDto(); }

            if (number.Equals(correctAnswer))
            {
                values.CurrentQuestionCounter = 0;

                speechOutput = ResourceMessage.CorrectAnswer;

                if (number02 < 10)
                {
                    speechOutput += $"{ResourceMessage.NextQuestionMessage} {this.GetQuestion(number01, ++number02)}";
                    correctAnswer = number01 * number02;
                }
                else
                {
                    speechOutput += $" {ResourceMessage.CongratsMessage}";
                    return MakeSkillResponse(speechOutput);
                }
            }
            else if (number02 >= 10)
            {
                number01 = number;
                number02 = 1;
                speechOutput = this.GetQuestion(number01, number02);
                correctAnswer = number01 * number02;
            }
            else
            {
                values.CurrentQuestionCounter++;
                speechOutput = $"{ResourceMessage.IncorrectAnswer} { this.GetQuestion(number01, number02)}";
            }

            values.PreviousAnswer = correctAnswer;
            values.Number01 = number01;
            values.Number02 = number02;

            return MakeSkillResponse($"{speechOutput}", false, values);
        }

        private string GetQuestion(int number01, int number02)
        {
            return $"¿Cúanto es {this.GetStringOfNumber(number01)} por {this.GetStringOfNumber(number02)}?";
        }

        private AttributesDto GetAttributes(SkillRequest input)
        {
            if(input?.Session?.Attributes != null)
            {
                return JsonConvert.DeserializeObject<AttributesDto>(input.Session.Attributes.First().Value.ToString());
            }

            return null;
        }

        private string GetStringOfNumber(int value)
        {
            if (value == 1) return "uno";
            if (value == 2) return "dos";
            if (value == 3) return "tres";
            if (value == 4) return "cuatro";
            if (value == 5) return "cinco";
            if (value == 6) return "seis";
            if (value == 7) return "siete";
            if (value == 8) return "ocho";
            if (value == 9) return "nueve";
            if (value == 10) return "diez";
            else return $"Numero fuera de rango {value}";
        }

        private int GetNumberOfString(string value)
        {
            if (value == "uno") return 1;
            if (value == "dos") return 2;
            if (value == "tres") return 3;
            if (value == "cuatro") return 4;
            if (value == "cinco") return 5;
            if (value == "seis") return 6;
            if (value == "siete") return 7;
            if (value == "ocho") return 8;
            if (value == "nueve") return 9;
            if (value == "diez") return 10;
            else return 0;
        }

        private SkillResponse MakeSkillResponse(string outputSpeech, bool shouldEndSession = false, AttributesDto triviaAttributes = null)
        {
            var speech = new SsmlOutputSpeech { Ssml = $"<speak>{outputSpeech}</speak>" };

            var response = new ResponseBody
            {
                OutputSpeech = speech,
                ShouldEndSession = shouldEndSession
            };

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = AlexaConstants.AlexaVersion
            };

            if (triviaAttributes != null)
            {
                var sessionAttributesTemp = new Dictionary<string, object> { { "AttributesDto", JsonConvert.SerializeObject(triviaAttributes) } };
                skillResponse.SessionAttributes = sessionAttributesTemp;

                skillResponse.Response.Directives.Add(new DialogElicitSlot(AlexaConstants.SlotName));
                skillResponse.Response.Reprompt = new Reprompt { OutputSpeech = speech };
            }

            return skillResponse;
        }

        /// <summary>
        /// logger interface
        /// </summary>
        /// <param name="text"></param>
        /// <returns>void</returns>
        private void Log(ILambdaContext context, string text)
        {
            if (context != null)
            {
                context.Logger.LogLine(text);
            }
        }
    }
}
