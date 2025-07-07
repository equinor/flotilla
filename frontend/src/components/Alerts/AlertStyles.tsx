import styled from 'styled-components'
import { TextAlignedButton } from 'components/Styles/StyledComponents'
import { Icon } from '@equinor/eds-core-react'

export const AlertContainer = styled.div`
    padding-left: 5px;
`

export const AlertButton = styled(TextAlignedButton)`
    :hover {
        background-color: #ff9797;
    }
`
export const StyledAlertIcon = styled(Icon)`
    width: 20px;
    height: 20px;
`
export const StyledAlertTitle = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

export const AlertIndent = styled.div`
    padding: 5px;
`
