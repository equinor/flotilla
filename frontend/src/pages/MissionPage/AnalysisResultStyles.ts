import { tokens } from '@equinor/eds-tokens'

const background = tokens.colors.ui.background__default.hex
const findingBackground = `color-mix(in srgb, ${tokens.colors.interactive.warning__resting.hex} 12%, ${background})`
const clearEdge = `color-mix(in srgb, ${tokens.colors.interactive.success__resting.hex} 65%, ${background})`

export const getAnalysisResultStyle = (hasFinding?: boolean) => ({
    background: hasFinding ? findingBackground : background,
    boxShadow: `inset 3px 0 ${
        hasFinding === undefined
            ? tokens.colors.ui.background__medium.hex
            : hasFinding
              ? tokens.colors.interactive.warning__resting.hex
              : clearEdge
    }`,
})
