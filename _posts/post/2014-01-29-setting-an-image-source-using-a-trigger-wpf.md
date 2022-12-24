---
title: Setting an image source using a trigger (WPF and Caliburn.Micro)
layout: post
date: 2014-01-29
category: archived
---

So all I'm doing here is creating a button that has a different images depending on the enum value that is bound to the control.

The images used are linked as resources in a resource project - the build action is `Resource`:

![Images are linked using a build action of Resource](https://snag.gy/o9v6X.jpg)

The button's view model (`ToggleMyEnumViewModel.cs`):

    public class ToggleMyEnumViewModel : Screen
    {
        MyEnum _myEnumProperty = MyEnum.Foo;

        public MyEnum MyEnumProperty
        {
            get { return _myEnumProperty; }
            set
            {
                _myEnumProperty = value;
                NotifyOfPropertyChange(() => MyEnumProperty);
            }
        }

        public void ToggleMyThing()
        {
            MyEnumProperty = MyEnumProperty == MyEnum.Foo
                ? MyEnum.Bar
                : MyEnum.Foo;
        }
    }

    public enum MyEnum {
        Foo, Bar
    }

The button's XAML (`ToggleMyEnumView.xaml`):

    <Button x:Class="MyNamespace.ToggleMyEnumButton"
                 xmlns="https://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="https://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:mc="https://schemas.openxmlformats.org/markup-compatibility/2006" 
                 xmlns:d="https://schemas.microsoft.com/expression/blend/2008"
                 xmlns:local="clr-namespace:MyNamespace"
                 xmlns:i="https://schemas.microsoft.com/expression/2010/interactivity"
                 mc:Ignorable="d" 
                 Cursor="Hand"
                 Background="Transparent">
        <Image Stretch="Uniform">
            <Image.Resources>
                <Style x:Key="XButtonStyle" TargetType="{x:Type Image}">
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding MyEnumProperty}" Value="{x:Static local:MyEnum.Foo}">
                            <Setter Property="Image.Source" Value="pack://application:,,,/MyProject.Assets;component/Images/foo.png" />
                        </DataTrigger>
                        <DataTrigger Binding="{Binding MyEnumProperty}" Value="{x:Static local:MyEnum.Bar}">
                            <Setter Property="Image.Source" Value="pack://application:,,,/MyProject.Assets;component/Images/bar.png" />
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Image.Resources>
        </Image>
 
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="Click">
            <cal:ActionMessage MethodName="ToggleMyThing"/>
        </i:EventTrigger>
    </i:Interaction.Triggers>

   </Button>

The `<cal:ActionMessage MethodName="ToggleMyThing"/>` part is what binds the button click to the `ToggleMyThing` method in the view model.

Things to note in the view's XAML:

- The style sits in the image's resources
- The target type of the style is set to the `Image` type
- The trigger has to be a data trigger
- The trigger just sets the `Source` property of the image based on the value of the `MyEnum` property

This is then used as a content control bound to an instance of a `ToggleMyEnumViewModel`. Consuming XAML:

    <ContentControl x:Name="ToggleMyEnum" />

Consuming view model:

    public ToggleMyEnumViewModel ToggleMyEnum { get; private set; }

    public ConsumingViewModel()
    {
        /// ...
        ToggleMyEnum = new ToggleMyEnumViewModel(); // or use an IOC factory
    }

This isn't exactly what I want, I would prefer to bind directly to a property on the consuming view model rather than indirectly with the `ToggleMyEnumViewModel`.
