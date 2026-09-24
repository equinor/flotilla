import { Button, Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled, { css } from 'styled-components'
import { getAnalysisResultStyle } from '../AnalysisResultStyles'

const spacing = tokens.spacings.comfortable
const divider = `1px solid ${tokens.colors.ui.background__medium.hex}`

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

export const ResultCard = styled(Card)<{ $hasAnalysis: boolean }>`
    && {
        display: grid;
        grid-row: 1 / span ${({ $hasAnalysis }) => ($hasAnalysis ? 3 : 2)};
        grid-template-rows: subgrid;
        min-width: 0;
        padding: 0;
        gap: 0;
        border-radius: 2px;
        border: ${divider};
    }
    > ${PreviewButton} {
        grid-row: 2;
    }
`

export const CardDetails = styled.div`
    grid-row: 1;
    display: flex;
    flex-direction: column;
    gap: ${spacing.small};
    padding: ${spacing.medium};
    overflow-wrap: anywhere;
    background: ${tokens.colors.ui.background__light.hex};
    border-bottom: ${divider};
    > p {
        font-weight: 400;
        color: ${tokens.colors.text.static_icons__secondary.hex};
    }
`

export const CardHeading = styled.div`
    display: flex;
    align-items: baseline;
    gap: ${spacing.small};
`

export const ResultTitle = styled(Typography)`
    min-width: 0;
    font-weight: 500;
`

export const TaskNumber = styled(Typography)`
    font-weight: 400;
    color: ${tokens.colors.text.static_icons__secondary.hex};
    white-space: nowrap;
    flex-shrink: 0;
    padding-left: ${spacing.small};
    border-left: ${divider};
`

export const AnalysisPreview = styled.div<{ $hasFinding: boolean }>`
    ${analysisHighlight}
    grid-row: 3;
    padding: ${spacing.medium};
    border-top: ${divider};
    display: flex;
    flex-direction: column;
    gap: ${spacing.medium_small};
`

export const ResultsSection = styled.section`
    min-width: 0;
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: ${spacing.medium};
`

export const Deck = styled.div`
    display: grid;
    grid-auto-flow: column;
    grid-auto-columns: 280px;
    grid-template-rows: auto auto auto;
    column-gap: ${spacing.medium};
    overflow-x: auto;
    align-items: stretch;
    padding: ${spacing.xx_small} ${spacing.xx_small} ${spacing.medium};
`
