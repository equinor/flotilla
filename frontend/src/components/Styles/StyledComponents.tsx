import { Button, Dialog, Pagination, Table } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled, { css } from 'styled-components'
import { content_widths } from 'utils/constants'

// For elements that cannot use PageContent because they bring their own layout, such
// as the EDS TopBar.
export const pageContentWidth = css`
    width: 100%;
    min-width: 0;
    margin-inline: auto;
    padding-inline: var(--eds-page-space-horizontal);
    ${content_widths.map((width) => `@media (min-width: ${width}) { max-width: ${width}; }`).join('\n')}
`

export const StyledDialog = styled(Dialog)`
    width: calc(100vw * 0.8);
    max-width: 420px;
    max-height: calc(100vh - 48px);
    overflow-y: auto;
    padding: 10px;
    display: flex;
    flex-direction: column;
`
StyledDialog.Actions = styled(Dialog.Actions)`
    display: flex;
    gap: 8px;
`
export const StyledAutoComplete = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: center;
    padding: 8px;
    gap: 25px;
    box-shadow: none;
`
export const StyledButton = styled(Button)`
    height: auto;
    min-height: ${tokens.shape.button.minHeight};
`
export const PageBackground = styled.div`
    display: flex;
    flex-direction: column;
    gap: 2rem;
    padding-block: var(--eds-page-space-vertical);
    flex: 1;
    background-color: ${tokens.colors.ui.background__light.hex};
`
// Siblings of this, inside PageBackground, run the full width of the window.
export const PageContent = styled.div`
    ${pageContentWidth}
    display: flex;
    flex-direction: column;
    gap: 2rem;
`
export const StyledLoading = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 1rem;
    padding-bottom: 1rem;
    gap: 1rem;
`
export const StyledPagination = styled(Pagination)`
    display: flex;
    height: 48px;
    padding: 0px 8px 0px 16px;
    background-color: ${tokens.colors.ui.background__default.hex};
`
export const StyledTable = styled(Table)`
    display: block;
    overflow: auto;
    max-width: 100%;
`

export const StyledTableAndMap = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: top;
    gap: 30px;
`

export const StyledTableCell = styled(Table.Cell)`
    && {
        font-family: Equinor, sans-serif;
        font-size: 0.65rem;
        font-weight: 600;
        letter-spacing: 0.1em;
        text-transform: uppercase;
        color: ${tokens.colors.text.static_icons__default.hex};
        background-color: ${tokens.colors.ui.background__default.hex};
        border-bottom: 2px solid ${tokens.colors.ui.background__medium.hex};
    }
`
export const StyledTableRow = styled(Table.Row)`
    transition: background-color 0.12s ease;
    &&:hover {
        background-color: ${tokens.colors.ui.background__light.hex};
    }
`
export const StyledTableBody = styled(Table.Body)`
    background-color: ${tokens.colors.ui.background__light.hex};
`
export const StyledTableCaption = styled(Table.Caption)`
    background-color: ${tokens.colors.ui.background__default.hex};
`
// Padding matches the page gutter so card text lines up with the bars above.
export const ContentCard = styled.div`
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: var(--eds-page-space-vertical) var(--eds-page-space-horizontal);
    border-radius: 2px;
    background: ${tokens.colors.ui.background__default.hex};
`

export const VideoStreamSection = styled(ContentCard)`
    display: grid;
`

export const cardShadow = '0 4px 12px rgba(0, 0, 0, 0.1), 0 12px 32px rgba(0, 0, 0, 0.08)'
export const subtleCardShadow = '0 2px 6px rgba(0, 0, 0, 0.08), 0 8px 20px rgba(0, 0, 0, 0.06)'

export const FieldLabel = styled.span`
    font-family: Equinor, sans-serif;
    font-size: 0.65rem;
    font-weight: 600;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
`
