# Changelog

## Fixes and Features Added in Version 4.72.0

- [#436 Changed how the next active document is picked on document close.](https://github.com/Dirkster99/AvalonDock/pull/436) (thanx to [FredrikS fredriks123](https://github.com/fredriks123))
- [#438 NullCheck for DragPoint](https://github.com/Dirkster99/AvalonDock/pull/438) (thanx to [Ben bbuerger](https://github.com/bbuerger))

## Fixes and Features Added in Version 4.72.0

- [#423 issue #422 DockingManager.LayoutItemTemplateSelector is applied twice because...](https://github.com/Dirkster99/AvalonDock/pull/423) (thanx to [Mona04](https://github.com/Mona04))

- [#425 Fix: Potential NRE on app close](https://github.com/Dirkster99/AvalonDock/pull/425) (thanx to [Khaos66](https://github.com/Khaos66))

- [#427 Fix floating windows still created twice](https://github.com/Dirkster99/AvalonDock/pull/427) (thanx to [Khaos66](https://github.com/Khaos66))

- [Add DockingManager.ShowNavigator](https://github.com/Dirkster99/AvalonDock/pull/428) (thanx to [Calum Robinson](https://github.com/calumr))

- [#431 Fix unwanted group orientation change when using mixed orientation](https://github.com/Dirkster99/AvalonDock/pull/431) (thanx to [KuroiRoy](https://github.com/KuroiRoy))

## Fixes Added in Version 4.71.2

- [#416 Fix Issue #226: Restore floating windows to maximized state](https://github.com/Dirkster99/AvalonDock/pull/416) (thanx to [Michael Möller](https://github.com/moellerm))

- [#417 Close and active selected item when NavigatorWindow is inactive](https://github.com/Dirkster99/AvalonDock/pull/417) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

- [#418 Add active content handler to LayoutAnchorableFloatingWindow and improve the active content handlers](https://github.com/Dirkster99/AvalonDock/pull/418) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

## Fixes Added in Version 4.71.1

- [#413 Fix the binding error in AnchorGroupTemplate](https://github.com/Dirkster99/AvalonDock/pull/413) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

- [#414 When apply new template, add back collection change event handler](https://github.com/Dirkster99/AvalonDock/pull/414) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

- [#415 Improved and fix floating window activation and activation pane](https://github.com/Dirkster99/AvalonDock/pull/415) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

## Fixes Added in Version 4.71.0

- [#399 add open/close LayoutFlayoutingWindowsControl events](https://github.com/Dirkster99/AvalonDock/pull/399) (thanx to [Denis Smirnov](https://github.com/GonzRu))

- [#400 set ResizeOverlay's owner always null](https://github.com/Dirkster99/AvalonDock/pull/400) (thanx to [Denis Smirnov](https://github.com/GonzRu))

- [#401 remove unused variable from DocumentPaneTabPanel](https://github.com/Dirkster99/AvalonDock/pull/401) (thanx to [Denis Smirnov](https://github.com/GonzRu))

- [#403 Add XmlSerializer cache to fix memory leaks.](https://github.com/Dirkster99/AvalonDock/pull/403) (thanx to [Pavel Kindruk](https://github.com/pkindruk))

- [#404 Fix deserialized layout document close.](https://github.com/Dirkster99/AvalonDock/pull/404) (thanx to [Pavel Kindruk](https://github.com/pkindruk))

- [#409 Restore previously activated document after closing active document](https://github.com/Dirkster99/AvalonDock/pull/409) (thanx to [L45eMy](https://github.com/L45eMy))

- [#410 Improved activation of floating Windows](https://github.com/Dirkster99/AvalonDock/pull/410) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

- [#411 Add anchorable hide and close notifications to DockingManager](https://github.com/Dirkster99/AvalonDock/pull/411) (thanx to [John Stewien](https://github.com/stewienj))

- [#412 Fix a issue where the dragged window still appeared above the overlay window](https://github.com/Dirkster99/AvalonDock/pull/412) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

## Fixes Added in Version 4.70.3

- [#394 Fix the get owner DockingManagerWindow and Update drag and drop](https://github.com/Dirkster99/AvalonDock/pull/394) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

- [#393 Add Null check for GetWindowChrome](https://github.com/Dirkster99/AvalonDock/pull/393) (thanx to [EQOH Noisrev](https://github.com/Noisrev))

- [#376 Prevents a known bug in WPF](https://github.com/Dirkster99/AvalonDock/pull/376) (thanx to [Ben Buerger](https://github.com/bbuerger))

## Fixes Added in Version 4.70.2

- [#338 Fixes #309 Anchorable Header not visible in generic theme](https://github.com/Dirkster99/AvalonDock/pull/338)   (thanx to [Darren Gosbell](https://github.com/dgosbell))
- [#346 fix crash if some assembly not allow GetTypes()](https://github.com/Dirkster99/AvalonDock/pull/346)   (thanx to [Trivalik](https://github.com/trivalik))
- [#347 #345 fix refresh when moving floating windows](https://github.com/Dirkster99/AvalonDock/pull/347)   (thanx to [Norberto Magni](https://github.com/nmagni))
- [#357 LayoutAutoHideWindowControl: UI automation name](https://github.com/Dirkster99/AvalonDock/pull/357)   (thanx to [Ben](https://github.com/bbuerger))
- [#363 Fixes #362 DockingManager with a Viewbox ancestor does not properly render auto-hidden LayoutAnchorables](https://github.com/Dirkster99/AvalonDock/pull/363)   (thanx to [Tim Cooke](https://github.com/timothylcooke))
- [#367 Fix for #306 StartDraggingFloatingWindow](https://github.com/Dirkster99/AvalonDock/pull/367)   (thanx to [Muhahe](https://github.com/Muhahe))

## Fixes Added in Version 4.70.1

- [#336 Keep ActiveContent when switching RootPanel](https://github.com/Dirkster99/AvalonDock/pull/336)   (thanx to [Khaos66](https://github.com/Khaos66))
- [#334 fix #333 x64-issue: x86-specific functions are used when project is compiled for x64-architecture](https://github.com/Dirkster99/AvalonDock/pull/334)   (thanx to [Jan cuellius](https://github.com/cuellius))

## Features and Fixes Added in Version 4.70.0

- [#331 FixDockAsDocument fix bug with CanExecute and Execute for DockAsDocument](https://github.com/Dirkster99/AvalonDock/pull/331)   (thanx to [askgthb](https://github.com/askgthb))
- [#328 NullCheck for currentActiveContent ](https://github.com/Dirkster99/AvalonDock/pull/328)   (thanx to [Ben bbuerger](https://github.com/bbuerger))
- [#327 Add default width and height of LayoutAnchorable](https://github.com/Dirkster99/AvalonDock/pull/327)   (thanx to [Anders Chen](https://github.com/AndersChen123))
- [#326 A more complete fix to per-monitor DPI issues](https://github.com/Dirkster99/AvalonDock/pull/326)   (thanx to [Robin rwg0](https://github.com/rwg0))
- [#324 Navigator Window Accessibility fixes](https://github.com/Dirkster99/AvalonDock/pull/324)   (thanx to [Siegfried Pammer](https://github.com/siegfriedpammer))

## Features and Fixes Added in Version 4.60.1

- [#314 Fix NavigatorWindow not working if there is only one document](https://github.com/Dirkster99/AvalonDock/pull/314)   (thanx to [Siegfried Pammer](https://github.com/siegfriedpammer))
- [#308 Code Clean-Up Serialization](https://github.com/Dirkster99/AvalonDock/pull/308)   (thanx to [RadvileSaveraiteFemtika](https://github.com/RadvileSaveraiteFemtika))
- [#317 Aded LayoutItem null check when processing mouseMiddleClickButton](https://github.com/Dirkster99/AvalonDock/pull/317)    (thanx to [JuanCar Orozco](https://github.com/Skaptor))

## Features and Fixes Added in Version 4.60.0

- [#278 Rename pt-BR to pt (make Brazilian Portuguese default to Portuguese)](https://github.com/Dirkster99/AvalonDock/pull/278)   (thanx to [mpondo](https://github.com/mpondo))
- [#272 Fix Mismatched ResourceKey on VS2013 Theme](https://github.com/Dirkster99/AvalonDock/pull/272)   (thanx to [Reisen Usagi](https://github.com/usagirei))
- [#274 Support custom styles for LayoutGridResizerControl](https://github.com/Dirkster99/AvalonDock/pull/274)   (thanx to [mpondo](https://github.com/mpondo))
- [#276 Support minimizing floating windows independently of main window](https://github.com/Dirkster99/AvalonDock/pull/276)   (thanx to [mpondo](https://github.com/mpondo))
- [#284 Vs2013 theme improvement](https://github.com/Dirkster99/AvalonDock/pull/284)   (thanx to [oktrue](https://github.com/oktrue))
- [#288 Fix close from taskbar for floating window](https://github.com/Dirkster99/AvalonDock/pull/288)   (thanx to [mpondo](https://github.com/mpondo))
- [#291 Fix Issue #281 floating window host: UI automation name](https://github.com/Dirkster99/AvalonDock/pull/291)   (thanx to [rmadsen-ks](https://github.com/rmadsen-ks))

## Features and Fixes Added in Version 4.51.1

- [#262 Contextmenus on dpi-aware application have a wrong scaling](https://github.com/Dirkster99/AvalonDock/issues/262)   (thanx to [moby42](https://github.com/moby42))
- [#259 Fixing problems with tests running with XUnit StaFact](https://github.com/Dirkster99/AvalonDock/pull/259)   (thanx to [Erik Ovegård](https://github.com/eriove))

- [#266 Adding a key for AnchorablePaneTitle](https://github.com/Dirkster99/AvalonDock/pull/266)   (thanx to [Zachary Canann](https://github.com/zcanann))

- [#267 Optional show hidden LayoutAnchorable on hover](https://github.com/Dirkster99/AvalonDock/pull/267)   (thanx to [Cory Todd](https://github.com/corytodd))

## Features Added in PRE-VIEW Version 4.51.0

- [#214 Migrate from netcoreapp3.0 to net5.0-windows](https://github.com/Dirkster99/AvalonDock/pull/214)  (thanx to [Magnus Lindhe](https://github.com/mgnslndh))

## Fixes added in Version 4.50.3

- [#163 IsSelected vs IsActive behavior changed from 3.x to 4.1/4.2?](https://github.com/Dirkster99/AvalonDock/issues/163) (thanx to [triman](https://github.com/triman))

- [#244 Right click on tab header closes tab unexpectedly](https://github.com/Dirkster99/AvalonDock/issues/244) (thanx to [Olly Atkins](https://github.com/oatkins))

- [#208 Maximized floating windows sit under the task bar](https://github.com/Dirkster99/AvalonDock/issues/208) (thanx to [Flynn1179](https://github.com/Flynn1179))

- [#255 Don't create FloatingWindows twice](https://github.com/Dirkster99/AvalonDock/pull/255) (thanx to [Khaos66](https://github.com/Khaos66))

## Fixes added in Version 4.50.2

- [#221 Default window style interfere with resizer window](https://github.com/Dirkster99/AvalonDock/issues/221) (thanx to [Magnus Lindhe](https://github.com/mgnslndh))
- ~~[#224 Reverted Fixed a bug that freezed when changing DocumentPane Orientation](https://github.com/Dirkster99/AvalonDock/pull/224) (thanx to [sukamoni](https://github.com/sukamoni))  
  See pull request for issues with this PR~~

- [#240 NullReferenceException in LayoutDocumentControl.OnModelChanged](https://github.com/Dirkster99/AvalonDock/issues/240) (thanx to [Khaos66](https://github.com/Khaos66))  
- [#225 Keyboard up/down in textbox in floating anchorable focusing DropDownControlArea](https://github.com/Dirkster99/AvalonDock/issues/225) (thanx to [Muhahe](https://github.com/Muhahe) [LyonJack](https://github.com/LyonJack) [bdachev](https://github.com/bdachev))
- [#229 Ensure DocumentPaneGroup (fix crash when documentpane on layoutGroup)](https://github.com/Dirkster99/AvalonDock/pull/229) (thanx to [sukamoni](https://github.com/sukamoni))  

## Fixes added in Version 4.50.1

- [#210 LayoutAnchorable with CanDockAsTabbedDocument="False" docks to LayoutDocumentPane when Pane is empty](https://github.com/Dirkster99/AvalonDock/issues/210) (thanx to [Łukasz Holetzke](https://github.com/goldie83))
- [#195 DocumentClosed event issue](https://github.com/Dirkster99/AvalonDock/issues/195) (thanx to [Skaptor](https://github.com/Skaptor))
- [#205 Fix issue where the ActiveContent binding doesn't update two ways when removing a document.](https://github.com/Dirkster99/AvalonDock/pull/205) (thanx to [PatrickHofman](https://github.com/PatrickHofman))

## Fixes added in Version 4.5

- [#199 Add to LayoutDocument CanHide property returning false](https://github.com/Dirkster99/AvalonDock/pull/199) (thanx to [bdachev](https://github.com/bdachev))
- [#138 Trying dock a floating window inside a document pane leads to its disappearing of window's content.](https://github.com/Dirkster99/AvalonDock/pull/138) (thanx to [cuellius](https://github.com/https://github.com/cuellius))
- [#197 [Bug] Tabs start getting dragged around if visual tree load times are too high](https://github.com/Dirkster99/AvalonDock/pull/138) (thanx to [X39](https://github.com/https://github.com/X39))
- [Bug fix for issue #194 App doesn't close after LayoutAnchorable AutoHide and docking it again](https://github.com/Dirkster99/AvalonDock/pull/203) (thanx to [sphet](https://github.com/https://github.com/sphet))

## Fixes & Features added in Version 4.4

- [#182 CanClose property of new LayoutAnchorableItem is different from its LayoutAnchorable](https://github.com/Dirkster99/AvalonDock/pull/183)  (thanx to [skyneps](https://github.com/skyneps))
- [#184 All documents disappear if document stops close application in Caliburn.Micro](https://github.com/Dirkster99/AvalonDock/issues/184)  (thanx to [ryanvs](https://github.com/ryanvs))

- Thanx to [bdachev](https://github.com/bdachev):  
  - [#186 Raise PropertyChanged notification when LayoutContent.IsFloating changes](https://github.com/Dirkster99/AvalonDock/pull/186) (ensure change of the [IsFloating](https://github.com/Dirkster99/AvalonDock/wiki/LayoutContent#properties) property when the Documents state changes)  
  - [#187 Allow to serialize CanClose if set to true for LayoutAnchorable instance](https://github.com/Dirkster99/AvalonDock/pull/187)  
  - [#188 Handle CanClose and CanHide in XAML](https://github.com/Dirkster99/AvalonDock/pull/188)  
  - [#190 Added additional check in LayoutGridControl.UpdateRowColDefinitions to avoid exception.](https://github.com/Dirkster99/AvalonDock/pull/190)  
  - [#192 Default MenuItem style not changed by VS2013 Theme](https://github.com/Dirkster99/AvalonDock/pull/192)


- Removed the additional [ToolTip](https://github.com/Dirkster99/AvalonDock/commit/5554de5c4bfadc37f974ba29803dc792b54f00d0) and [ContextMenu](https://github.com/Dirkster99/AvalonDock/commit/103e1068bc9f5bae8fef275a0e785393b4115764) styles from the Generic.xaml in VS2013 [more details here](https://github.com/Dirkster99/AvalonDock/pull/170#issuecomment-674253874)
- [#189 Removal of DictionaryTheme breaks my application](https://github.com/Dirkster99/AvalonDock/issues/189)  (thanx to [hamohn](https://github.com/hamohn))

## Fixes & Features added in Version 4.3

- Localized labels in [NavigatorWindow](https://github.com/Dirkster99/AvalonDock/wiki/NavigatorWindow)

- [#170 Several Improvements](https://github.com/Dirkster99/AvalonDock/pull/170) (thanx to [刘晓青 LyonJack](https://github.com/LyonJack))  
  - Improved VS 2013 Theme and ease of reusing controls  
  - [Fix Issue #85 Floating Window Title Flashing](https://github.com/Dirkster99/AvalonDock/issues/85)  
  - [Fix Issue #71 Hiding and showing anchorable in document's pane throws an exception](https://github.com/Dirkster99/AvalonDock/issues/71)  
  - [Fix Issue #135 ActiveContent not switching correctly for floating window](https://github.com/Dirkster99/AvalonDock/issues/135)  
  - [Fix Issue #165 ActiveContent not stable](https://github.com/Dirkster99/AvalonDock/issues/165)  
  - [Fix Issue #171 LayoutDocument leaks on close](https://github.com/Dirkster99/AvalonDock/issues/171)  
  - **Breaking Change**  
    [Fix Issue #174 The SetWindowSizeWhenOpened Feature is broken](https://github.com/Dirkster99/AvalonDock/issues/174)
  - [Fix Issue #177 ToolBar TabItem color error](https://github.com/Dirkster99/AvalonDock/issues/177)

- [#59 InvalidOperationException when deserializing layout](https://github.com/Dirkster99/AvalonDock/issues/59#issuecomment-642934204)

- [#136 Layout "locking" method for Anchorables (tool windows) Part II via Style of LayoutAnchorableItem](https://github.com/Dirkster99/AvalonDock/issues/136)

- [#136 Layout "locking" method for Anchorables (tool windows) Part III Added CanDock for LayoutAnchorable and LayoutDocument](https://github.com/Dirkster99/AvalonDock/issues/136)
    [commit 6b611fa7fdce4f6dcfed1cf00c3b9193000ffe16](https://github.com/Dirkster99/AvalonDock/commit/6b611fa7fdce4f6dcfed1cf00c3b9193000ffe16)

- [#169 - Autohide LayoutAnchorable causes CPU load on idle](https://github.com/Dirkster99/AvalonDock/issues/169)

## Fixes & Features  added in Version 4.2

- [#136 Layout "locking" method for Anchorables (tool windows)](https://github.com/Dirkster99/AvalonDock/issues/136)

- [# 159 Docking manager in TabControl can cause InvalidOperationException](https://github.com/Dirkster99/AvalonDock/issues/159)

- [# 151 Model.Root.Manager may be null in LayoutDocumentTabItem](https://github.com/Dirkster99/AvalonDock/issues/151) Thanx to [scdmitryvodich](https://github.com/scdmitryvodich)

## Fixes & Features  added in Version 4.1

- [Fix #137 BindingExpression in VS2013 theme](https://github.com/Dirkster99/AvalonDock/issues/137)

- [Feature Added: Auto resizing floating window to content](https://github.com/Dirkster99/AvalonDock/pull/146) [thanx to Erik Ovegård](https://github.com/eriove)

- Feature Added: Virtualizing Tabbed Documents and/or LayoutAnchorables [PR #143](https://github.com/Dirkster99/AvalonDock/pull/143) + [Virtualization Options](https://github.com/Dirkster99/AvalonDock/commit/1a45dbbe66c931e6c87ad769a9b269da4cb290ae)  [thanx to matko238](https://github.com/matko238)  
  - See ``DockingManager.IsVirtualizingAnchorable``, ``DockingManager.IsVirtualizingDocument``, and ``IsVirtualizing`` property on ``LayoutAnchorablePaneControl`` and ``LayoutDocumentPaneControl``.

- [Fixed Issue #149 Flicker/Lag when restoring floating window from Maximized state](https://github.com/Dirkster99/AvalonDock/issues/149) [thanx to skyneps](https://github.com/skyneps)

- [Fixed Issue #150 Restoring floating window position on multiple monitors uses wrong Point for Virtual Screen location](https://github.com/Dirkster99/AvalonDock/issues/150) [thanx to charles-roberts](https://github.com/charles-roberts)

## Fixes and Features added in Version 4.0

- [Fix #98 with floating window without a content #99](https://github.com/Dirkster99/AvalonDock/pull/99) Thanx to [scdmitryvodich](https://github.com/scdmitryvodich)

- Changed coding style to using TABS as indentation
- **Breaking Change** [Changed namespaces to AvalonDock (as authored originally in version 2.0 and earlier)](https://github.com/Dirkster99/AvalonDock/pull/102) See also [Issue #108](https://github.com/Dirkster99/AvalonDock/issues/108)

- [Fix #101 and new fix for #81 with docked pane becomes not visible.](https://github.com/Dirkster99/AvalonDock/issues/101) Thanx to [scdmitryvodich](https://github.com/scdmitryvodich)

- [Feature added: allow documents to be docked in a floating window](https://github.com/Dirkster99/AvalonDock/pull/107) Thanx to [amolf-se](https://github.com/amolf-se) [https://github.com/mkonijnenburg](mkonijnenburg) @ [http://www.amolf.nl](http://www.amolf.nl)

- [Feature added: AutoHideDelay property to control the time until an AutoHide window is reduced back to its anchored representation](https://github.com/Dirkster99/AvalonDock/pull/110) Thanx to [Alexei Stukov](https://github.com/Jiiks)

- [Fix #127 Controls cause memory leaks via event listener](https://github.com/Dirkster99/AvalonDock/issues/127)

- [Fix #111 AvalonDock.LayoutRoot doesn't know how to deserialize...](https://github.com/Dirkster99/AvalonDock/issues/111) Thanx to [scdmitryvodich](https://github.com/scdmitryvodich)

- [Fix #117 Dragging LayoutAnchoreable into outer docking buttons of floating document result in Exception](https://github.com/Dirkster99/AvalonDock/issues/117) Thanx to [scdmitryvodich](https://github.com/scdmitryvodich)

- [Fix #132 Drop FloatingDocumentWindow into DocumentPane is not consistent (when FloatingDocumentWindow contains LayoutAnchorable)](https://github.com/Dirkster99/AvalonDock/issues/132)

## More Patch History
Please review the **Path History** for more more information on patches and feaures in [previously released versions of AvalonDock](https://github.com/Dirkster99/AvalonDock/wiki/Patch-History).
