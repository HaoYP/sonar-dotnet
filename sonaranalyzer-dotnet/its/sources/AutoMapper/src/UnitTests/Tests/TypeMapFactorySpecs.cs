using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Shouldly;
using AutoMapper.Configuration.Conventions;

namespace AutoMapper.UnitTests.Tests
{
    using System;
    using Assembly = System.Reflection.Assembly;

    public class StubNamingConvention : INamingConvention
    {
        private readonly Func<Match, string> _replaceFunc;

        public StubNamingConvention(Func<Match, string> replaceFunc)
        {
            _replaceFunc = replaceFunc;
            SeparatorCharacter = "";
        }

        public Regex SplittingExpression { get; set; }
        public string SeparatorCharacter { get; set; }

        public string ReplaceValue(Match match)
        {
            return _replaceFunc(match);
        }
    }

    public class When_constructing_type_maps_with_matching_property_names : SpecBase
    {
        private TypeMapFactory _factory;

        public class Source
        {
            public int Value { get; set; }
            public int SomeOtherValue { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }
            public int SomeOtherValue { get; set; }
        }

        protected override void Establish_context()
        {
            _factory = new TypeMapFactory();
        }

        private class TestProfile : Profile
        {
            public override string ProfileName => "Test";
        }

        [Fact]
        public void Should_map_properties_with_same_name()
        {
            var mappingOptions = new TestProfile();
            //mappingOptions.SourceMemberNamingConvention = new PascalCaseNamingConvention();
            //mappingOptions.DestinationMemberNamingConvention = new PascalCaseNamingConvention();
            var profile = new ProfileMap(mappingOptions);

            var typeMap = _factory.CreateTypeMap(typeof(Source), typeof(Destination), profile);

            var propertyMaps = typeMap.GetPropertyMaps();

            propertyMaps.Count().ShouldBe(2);
        }
    }

    public class When_using_a_custom_source_naming_convention : SpecBase
    {
        private TypeMapFactory _factory;
        private TypeMap _map;
        private ProfileMap _mappingOptions;
        
        private class Source
        {
            public SubSource some__source { get; set; }
        }

        private class SubSource
        {
            public int value { get; set; }
        }

        private class Destination
        {
            public int SomeSourceValue { get; set; }
        }

        private class TestProfile : Profile
        {
            public override string ProfileName => "Test";
        }
        protected override void Establish_context()
        {
            var namingConvention = new StubNamingConvention(s => s.Value.ToLower()){SeparatorCharacter = "__", SplittingExpression = new Regex(@"[\p{Ll}\p{Lu}0-9]+(?=__?)")};

            var profile = new TestProfile();
            profile.AddMemberConfiguration().AddMember<NameSplitMember>(_ =>
            {
                _.SourceMemberNamingConvention = namingConvention;
                _.DestinationMemberNamingConvention = new PascalCaseNamingConvention();
            });
            _mappingOptions = new ProfileMap(profile);

            _factory = new TypeMapFactory();

        }

        protected override void Because_of()
        {
            _map = _factory.CreateTypeMap(typeof(Source), typeof(Destination), _mappingOptions);
        }

        [Fact]
        public void Should_split_using_naming_convention_rules()
        {
            _map.GetPropertyMaps().Count().ShouldBe(1);
        }
    }

    public class When_using_a_custom_destination_naming_convention : SpecBase
    {
        private TypeMapFactory _factory;
        private TypeMap _map;
        private ProfileMap _mappingOptions;

        private class Source
        {
            public SubSource SomeSource { get; set; }
        }

        private class SubSource
        {
            public int Value { get; set; }
        }

        private class Destination
        {
            public int some__source__value { get; set; }
        }

        private class TestProfile : Profile
        {
            public override string ProfileName => "Test";
        }

        protected override void Establish_context()
        {
            var namingConvention = new StubNamingConvention(s => s.Value.ToLower()) { SeparatorCharacter = "__", SplittingExpression = new Regex(@"[\p{Ll}\p{Lu}0-9]+(?=__?)") };

            var profile = new TestProfile();
            profile.AddMemberConfiguration().AddMember<NameSplitMember>(_ =>
            {
                _.SourceMemberNamingConvention = new PascalCaseNamingConvention();
                _.DestinationMemberNamingConvention = namingConvention;
            });
            _mappingOptions = new ProfileMap(profile);

            _factory = new TypeMapFactory();
        }

        protected override void Because_of()
        {
            _map = _factory.CreateTypeMap(typeof(Source), typeof(Destination), _mappingOptions);
        }

        [Fact]
        public void Should_split_using_naming_convention_rules()
        {
            _map.GetPropertyMaps().Count().ShouldBe(1);
        }
    }

    public class When_using_a_source_member_name_replacer : SpecBase
    {
        private TypeMapFactory _factory;

        public class Source
        {
            public int Value { get; set; }
            public int �v�ator { get; set; }
            public int SubAirlinaFlight { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }
            public int Aviator { get; set; }
            public int SubAirlineFlight { get; set; }
        }

        protected override void Establish_context()
        {
            _factory = new TypeMapFactory();
        }

        [Fact]
        public void Should_map_properties_with_different_names()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.ReplaceMemberName("A", "�");
                cfg.ReplaceMemberName("i", "�");
                cfg.ReplaceMemberName("Airline", "Airlina");
                cfg.CreateMap<Source, Destination>();
            });

            var mapper = config.CreateMapper();
            var dest = mapper.Map<Destination>(new Source {�v�ator = 3, SubAirlinaFlight = 4, Value = 5});
            dest.Aviator.ShouldBe(3);
            dest.SubAirlineFlight.ShouldBe(4);
            dest.Value.ShouldBe(5);
        }
    }
}
