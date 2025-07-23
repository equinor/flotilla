import { Button, Dialog, Table, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { phone_width } from 'utils/constants'

export const StyledDialog = styled(Dialog)`
    width: calc(100vw * 0.8);
    max-width: 420px;
    padding: 10px;
    display: flex;
    flex-direction: column;
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
export const TextAlignedButton = styled(Button)`
    text-align: left;
    height: auto;
    padding: 5px 5px;
`
export const StyledPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
    padding: 2rem;
    @media (max-width: ${phone_width}) {
        padding: 0.7rem;
    }
    min-height: calc(100vh - 65px);
    background-color: ${tokens.colors.ui.background__light.hex};
`
export const AttributeTitleTypography = styled(Typography)`
    variant: meta;
    fontsize: 14;
    color: ${tokens.colors.text.static_icons__secondary.hex};
`
export const StyledTableCell = styled(Table.Cell)`
    background-color: ${tokens.colors.ui.background__default.hex};
`
export const StyledTableBody = styled(Table.Body)`
    background-color: ${tokens.colors.ui.background__light.hex};
`
export const StyledTableCaption = styled(Table.Caption)`
    background-color: ${tokens.colors.ui.background__default.hex};
`
