using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Flavio.FunctionTest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Graph;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AzureFunctionsSample.Test
{
    public class FunctionForTestTests
    {
        private readonly IFixture _fixture;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IGraphClientService _graphClientService;
        private readonly FunctionForTest functionForTest;

        public FunctionForTestTests()
        {
            _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            _logger = Substitute.For<ILogger>();
            _configuration = Substitute.For<IConfiguration>();
            _graphClientService = Substitute.For<IGraphClientService>();

            PrepareTestEnv();

            functionForTest = new FunctionForTest(_configuration, _graphClientService);
        }

        private void PrepareTestEnv()
        {
            _configuration["ClientId"].Returns(_fixture.Create<string>());
            _configuration["ClientSecret"].Returns(_fixture.Create<string>());
            _configuration["TenantId"].Returns(_fixture.Create<string>());

            var userGraphUserCollectionPage = Substitute.For<IGraphServiceUsersCollectionPage>();
            userGraphUserCollectionPage.CurrentPage.Returns(TestUsers(5));
            var userCollection = Substitute.For<IGraphServiceUsersCollectionRequest>();
            userCollection.GetAsync().Returns(userGraphUserCollectionPage);
            var users = Substitute.For<IGraphServiceUsersCollectionRequestBuilder>();
            users.Request().Returns(userCollection);
            var graph = Substitute.ForPartsOf<GraphServiceClient>(Substitute.For<IAuthenticationProvider>(), Substitute.For<IHttpProvider>());
            graph.Users.Returns(users);

            
            _graphClientService
                .GetGraphClient(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string[]>())
                .Returns(graph);
        }

        private IEnumerable<User> TestUsers(int quantity)
        {
            var result = new List<User>();
            for (int i = 0; i < quantity; i++)
            {
                result.Add(
                    new User()
                    {
                        DisplayName = _fixture.Create<string>(),
                        Id = _fixture.Create<string>(),
                        GivenName = _fixture.Create<string>(),
                        Surname = _fixture.Create<string>()
                    });
            }
            return result;
        }

        [Fact]
        public async Task Get_TenantIdNotConfigured_ReturnBadRequest()
        {
            _configuration["TenantId"].Returns<string>(x => null);

            var request = GenerateHttpRequest(2);
            request.Method = "GET";
            var response = await functionForTest.Run(request, _logger);

            response.ShouldNotBeNull();
            OkObjectResult? objectResult = response as OkObjectResult;
            objectResult?.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task Get_WithValidParameters_ReturnSuccess()
        {
            var request = GenerateHttpRequest(2);
            request.Method = "GET";
            var response = await functionForTest.Run(request, _logger);

            response.ShouldNotBeNull();
            OkObjectResult? objectResult = response as OkObjectResult;
            objectResult?.StatusCode.ShouldBe(StatusCodes.Status200OK);
            IEnumerable<object>? resultValue = objectResult?.Value as IEnumerable<object>;
            resultValue?.ToList().Count.ShouldBe(2);
        }

        private DefaultHttpRequest GenerateHttpRequest(object number)
        {
            var request = new DefaultHttpRequest(new DefaultHttpContext());
            var queryParams = new Dictionary<string, StringValues>() { { "max", number.ToString() } };
            request.Query = new QueryCollection(queryParams);
            return request;
        }
    }
}