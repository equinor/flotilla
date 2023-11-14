import { Dialog } from '@equinor/eds-core-react'
import styled from 'styled-components'

export const StyledDialog = styled(Dialog)`
    display: flex;
    flex-direction: column;
    padding: 1rem;
    width: 480px;
`

export const HorizontalContent = styled.div`
    display: flex;
    gap: 1rem;
`

export const VerticalContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`
