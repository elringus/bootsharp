---
layout: home

title: Use C# in web apps with comfort
titleTemplate: Bootsharp • :title

hero:
  name: Bootsharp
  text: Use C# in web apps with comfort
  tagline: Single-file ES module, auto-generated JavaScript bindings and type definitions.
  actions:
    - theme: brand
      text: Get Started
      link: /guide/
    - theme: alt
      text: View on GitHub
      link: https://github.com/elringus/bootsharp
  image:
    src: /favicon.svg
    alt: Bootsharp
---

<style>
:root {
  --vp-home-hero-name-color: transparent;
  --vp-home-hero-name-background: -webkit-linear-gradient(120deg, #bd34fe 30%, #41d1ff);

  --vp-home-hero-image-background-image: linear-gradient(75deg, #bd34fe 40%, #47caff 50%);
  --vp-home-hero-image-filter: blur(44px);
}

@media (min-width: 640px) {
  :root {
    --vp-home-hero-image-filter: blur(56px);
  }
}

@media (min-width: 960px) {
  :root {
    --vp-home-hero-image-filter: blur(68px);
  }
}

.VPHome .VPButton.medium.brand,
.VPHome .VPButton.medium.alt {
    position: relative;
    display: flex;
    align-items: center;
    padding-top: 5px;
    padding-bottom: 5px;
    padding-right: 15px;
}

.VPHome .VPButton.medium.brand::after {
    content: "";
    mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='22' height='22' viewBox='0 0 24 24' fill='currentColor'%3E%3Cpath d='M17.92 11.62a1.001 1.001 0 0 0-.21-.33l-5-5a1.003 1.003 0 1 0-1.42 1.42l3.3 3.29H7a1 1 0 0 0 0 2h7.59l-3.3 3.29a1.002 1.002 0 0 0 .325 1.639 1 1 0 0 0 1.095-.219l5-5a1 1 0 0 0 .21-.33 1 1 0 0 0 0-.76Z'%3E%3C/path%3E%3C/svg%3E") no-repeat 50% 50%;
    /* Required to render correctly on mobile. */
    display: inline-block;
    width: 22px;
    height: 22px;
    padding-left: 30px;
    background-color: var(--vp-button-brand-text);
}

.VPHome .VPButton.medium.alt::after {
    content: "";
    mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 24 24' fill='currentColor'%3E%3Cpath d='M19.33 10.18a1 1 0 0 1-.77 0 1 1 0 0 1-.62-.93l.01-1.83-8.2 8.2a1 1 0 0 1-1.41-1.42l8.2-8.2H14.7a1 1 0 0 1 0-2h4.25a1 1 0 0 1 1 1v4.25a1 1 0 0 1-.62.93Z'%3E%3C/path%3E%3Cpath d='M11 4a1 1 0 1 1 0 2H7a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1v-4a1 1 0 1 1 2 0v4a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3V7a3 3 0 0 1 3-3h4Z'%3E%3C/path%3E%3C/svg%3E") no-repeat 50% 50%;
    /* Required to render correctly on mobile. */
    display: inline-block;
    width: 20px;
    height: 20px;
    padding-left: 32px;
    background-color: var(--vp-button-alt-text);
}
</style>
