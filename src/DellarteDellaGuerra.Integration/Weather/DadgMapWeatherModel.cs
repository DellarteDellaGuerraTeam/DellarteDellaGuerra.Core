using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.Integration.Weather
{
    public sealed class DadgMapWeatherModel : MapWeatherModel
    {
        private readonly MapWeatherModel _inner;

        public DadgMapWeatherModel(MapWeatherModel inner) => _inner = inner;

        public override TaleWorlds.CampaignSystem.CampaignTime WeatherUpdateFrequency => _inner.WeatherUpdateFrequency;

        public override TaleWorlds.CampaignSystem.CampaignTime WeatherUpdatePeriod => _inner.WeatherUpdatePeriod;

        public override AtmosphereState GetInterpolatedAtmosphereState(TaleWorlds.CampaignSystem.CampaignTime timeOfYear, Vec3 pos) =>
            _inner.GetInterpolatedAtmosphereState(timeOfYear, ClampToWeatherGrid(pos));

        public override void GetSeasonTimeFactorOfCampaignTime(TaleWorlds.CampaignSystem.CampaignTime ct, out float timeFactorForSnow, out float timeFactorForRain, bool snapCampaignTimeToWeatherPeriod = true) =>
            _inner.GetSeasonTimeFactorOfCampaignTime(ct, out timeFactorForSnow, out timeFactorForRain, snapCampaignTimeToWeatherPeriod);

        public override void InitializeCaches() => _inner.InitializeCaches();

        public override WeatherEvent GetWeatherEventInPosition(Vec2 pos) =>
            _inner.GetWeatherEventInPosition(ClampToWeatherGrid(pos));

        public override WeatherEventEffectOnTerrain GetWeatherEffectOnTerrainForPosition(Vec2 pos) =>
            _inner.GetWeatherEffectOnTerrainForPosition(ClampToWeatherGrid(pos));

        public override WeatherEvent UpdateWeatherForPosition(CampaignVec2 position, TaleWorlds.CampaignSystem.CampaignTime ct) =>
            _inner.UpdateWeatherForPosition(ClampToWeatherGrid(position), ct);

        public override AtmosphereInfo GetAtmosphereModel(CampaignVec2 position) =>
            _inner.GetAtmosphereModel(ClampToWeatherGrid(position));

        public override void GetSnowAndRainDataForPosition(Vec2 position, TaleWorlds.CampaignSystem.CampaignTime ct, out float snowValue, out float rainValue) =>
            _inner.GetSnowAndRainDataForPosition(ClampToWeatherGrid(position), ct, out snowValue, out rainValue);

        public override Vec2 GetWindForPosition(CampaignVec2 position) =>
            _inner.GetWindForPosition(ClampToWeatherGrid(position));

        private static CampaignVec2 ClampToWeatherGrid(CampaignVec2 position) =>
            new CampaignVec2(ClampToWeatherGrid(position.ToVec2()), position.IsOnLand);

        private static Vec3 ClampToWeatherGrid(Vec3 position)
        {
            var clamped = ClampToWeatherGrid(new Vec2(position.x, position.y));
            return new Vec3(clamped.x, clamped.y, position.z);
        }

        private static Vec2 ClampToWeatherGrid(Vec2 position)
        {
            var mapScene = Campaign.Current?.MapSceneWrapper;
            if (mapScene == null) return position;

            var terrainSize = mapScene.GetTerrainSize();
            return new Vec2(
                Math.Max(0f, Math.Min(position.x, terrainSize.x - 0.001f)),
                Math.Max(0f, Math.Min(position.y, terrainSize.y - 0.001f)));
        }
    }
}
