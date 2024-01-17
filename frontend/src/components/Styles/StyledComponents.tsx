import { Button, Dialog } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'

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
