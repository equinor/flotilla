import { Dialog } from '@equinor/eds-core-react'
import styled from 'styled-components'

export const StyledDialog = styled(Dialog)`
    display: flex;
    flex-direction: column;
    padding: 1rem;
    width: calc(100vw * 0.7);
    max-width: 500px;
    max-height: 90vh;
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
