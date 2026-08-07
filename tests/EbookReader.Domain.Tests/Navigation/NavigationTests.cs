namespace EbookReader.Domain.Tests.Navigation;

public sealed class NavigationTests
{
    [Fact]
    public void NavigationRequiresLabel()
    {
        ReadingLocation target = ReadingLocation.AtSectionStart(new SectionId("s1"));

        Assert.Throws<ArgumentException>(() => new NavigationItem(" ", target));
    }

    [Fact]
    public void NavigationSnapshotsChildren()
    {
        ReadingLocation target = ReadingLocation.AtSectionStart(new SectionId("s1"));
        List<NavigationItem> children = [new NavigationItem("Child", target)];
        NavigationItem root = new("Root", target, children);

        children.Clear();

        Assert.Single(root.Children);
    }

    [Fact]
    public void EmptyTableOfContentsIsReusable()
    {
        Assert.Empty(TableOfContents.Empty.Items);
    }
}

public sealed class NavigationGroupingTests
{
    [Fact]
    public void NavigationItemAllowsTargetlessGroupingWhenChildrenExist()
    {
        SectionId sectionId = new("section");
        NavigationItem child = new("Child", ReadingLocation.AtSectionStart(sectionId));

        NavigationItem group = new("Part One", target: null, [child]);

        Assert.Null(group.Target);
        Assert.Single(group.Children);
    }

    [Fact]
    public void NavigationItemRejectsTargetlessLeaf()
    {
        Assert.Throws<ArgumentException>(() => new NavigationItem("Empty group", target: null));
    }
}
