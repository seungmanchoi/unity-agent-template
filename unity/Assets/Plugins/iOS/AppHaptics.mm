// One-shot impact haptics for Platform/Haptics.cs (kind: 0 light, 1 medium, 2 heavy, 3 rigid).
// The symbol name is app-neutral so renaming the namespace never touches native code.
#import <UIKit/UIKit.h>

extern "C" void AppHaptic(int kind)
{
    UIImpactFeedbackStyle style = kind == 0 ? UIImpactFeedbackStyleLight
        : kind == 1 ? UIImpactFeedbackStyleMedium
        : kind == 2 ? UIImpactFeedbackStyleHeavy
        : UIImpactFeedbackStyleRigid;
    UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
    [generator impactOccurred];
}
