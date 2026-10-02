import { Button, Dialog, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled, { css } from 'styled-components'
import { phone_width } from 'utils/constants'
import { getAnalysisResultStyle } from '../AnalysisResultStyles'

const spacing = tokens.spacings.comfortable
const divider = `1px solid ${tokens.colors.ui.background__medium.hex}`
const galleryStackBreakpoint = '900px'

const analysisHighlight = css<{ $hasFinding?: boolean }>`
    ${({ $hasFinding }) => $hasFinding !== undefined && getAnalysisResultStyle($hasFinding)}
`

export const PreviewButton = styled(Button)`
    && {
        display: block;
        width: 100%;
        height: auto;
        min-height: 0;
        padding: 0;
        text-align: left;
        white-space: normal;
        border-radius: 0;
        background: transparent;
        color: ${tokens.colors.text.static_icons__default.hex};
    }
    > span {
        display: block;
        width: 100%;
    }
    &:focus-visible {
        outline: 2px solid ${tokens.colors.interactive.primary__resting.hex};
        outline-offset: -2px;
    }
`

export const ResultTitle = styled(Typography)`
    min-width: 0;
    font-weight: 500;
`

const TaskNumber = styled(Typography)`
    font-weight: 400;
    color: ${tokens.colors.text.static_icons__secondary.hex};
    white-space: nowrap;
    flex-shrink: 0;
    padding-left: ${spacing.small};
    border-left: ${divider};
`

export const Gallery = styled(Dialog)`
    --gallery-viewport-gap: ${spacing.xxx_large};
    display: block;
    box-sizing: border-box;
    width: min(1280px, calc(100vw - var(--gallery-viewport-gap)));
    max-width: calc(100vw - var(--gallery-viewport-gap));
    max-height: calc(100dvh - var(--gallery-viewport-gap));
    overflow-y: auto;
    padding: 0;
    @media (max-width: ${phone_width}) {
        --gallery-viewport-gap: ${spacing.large};
    }
`

export const GalleryHeader = styled.div`
    position: sticky;
    top: 0;
    z-index: 1;
    background: ${tokens.colors.ui.background__light.hex};
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    justify-content: space-between;
    gap: ${spacing.medium};
    padding: 20px ${spacing.large};
    border-bottom: ${divider};
`

export const GalleryDetails = styled.div`
    flex: 1 1 260px;
    min-width: 0;
    overflow-wrap: anywhere;
    display: flex;
    flex-direction: column;
    gap: ${spacing.small};
`

export const GalleryHeading = styled.div`
    display: flex;
    align-items: baseline;
    gap: ${spacing.medium};
`

export const GalleryTaskNumber = styled(TaskNumber)`
    padding-left: ${spacing.medium};
`

export const GalleryDescription = styled(Typography)`
    max-width: 72ch;
    color: ${tokens.colors.text.static_icons__secondary.hex};
`

export const Navigation = styled.div`
    display: flex;
    align-items: center;
    gap: ${spacing.small};
`

export const GalleryBody = styled.div<{ $paired: boolean }>`
    display: grid;
    grid-template-columns: ${({ $paired }) => ($paired ? 'minmax(0, 1fr) 300px' : 'minmax(0, 1fr)')};
    gap: ${spacing.large};
    padding: ${spacing.large};
    @media (max-width: ${galleryStackBreakpoint}) {
        grid-template-columns: minmax(0, 1fr);
        padding: ${spacing.medium};
    }
`

export const AnalysisContent = styled.div<{ $hasFinding?: boolean }>`
    display: flex;
    flex-direction: column;
    gap: ${spacing.medium_small};
    min-width: 0;
    ${analysisHighlight}
    padding: ${({ $hasFinding }) => ($hasFinding === undefined ? '0' : spacing.medium)};
    border-radius: 2px;
`

export const Companion = styled.aside`
    display: flex;
    flex-direction: column;
    gap: ${spacing.medium};
    min-width: 0;
    padding-left: ${spacing.large};
    border-left: ${divider};
    @media (max-width: ${galleryStackBreakpoint}) {
        padding: ${spacing.medium} 0 0;
        border-left: none;
        border-top: ${divider};
    }
`

export const Metadata = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: ${spacing.medium} ${spacing.x_large};
    padding: ${spacing.medium} ${spacing.large};
    border-top: ${divider};
`

export const Strip = styled.nav`
    background: ${tokens.colors.ui.background__light.hex};
    padding: ${spacing.medium} ${spacing.large};
    border-top: ${divider};
    > div {
        display: flex;
        gap: ${spacing.medium_small};
        overflow-x: auto;
        padding: ${spacing.medium_small} ${spacing.xx_small} ${spacing.x_small};
    }
`

export const StripButton = styled(Button)<{ $hasFinding?: boolean }>`
    && {
        flex: 0 0 180px;
        height: auto;
        padding: ${spacing.medium_small};
        text-align: left;
        white-space: normal;
        background: ${tokens.colors.ui.background__default.hex};
        color: ${tokens.colors.text.static_icons__default.hex};
        border: ${divider};
        box-shadow: ${({ $hasFinding }) => getAnalysisResultStyle($hasFinding).boxShadow};
    }
    &[aria-current='true'] {
        outline: 2px solid ${tokens.colors.interactive.primary__resting.hex};
        outline-offset: -2px;
    }
    > span {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        width: 100%;
        gap: ${spacing.small};
    }
`
