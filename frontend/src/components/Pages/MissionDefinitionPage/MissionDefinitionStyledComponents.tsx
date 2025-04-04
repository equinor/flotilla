import { Button, Card, Dialog } from '@equinor/eds-core-react'
import styled from 'styled-components'

export const FormCard = styled(Card)`
    display: flex;
    justify-content: center;
    padding: 8px;
    gap: 25px;
    box-shadow: none;
`

export const ButtonSection = styled.div`
    display: flex;
    margin-left: auto;
    gap: 10px;
`

export const FormContainer = styled.div`
    display: flex;
    gap: 10px 20px;

    @media (min-width: 800px) {
        display: grid;
        grid-template-columns: repeat(2, 480px);
    }
`

export const FormItem = styled.div`
    width: 100%;
    height: auto;
    word-break: break-word;
    hyphens: auto;
    min-height: 80px;
`

export const StyledDialog = styled(Dialog)`
    display: flex;
    justify-content: space-between;
    padding: 1rem;
    width: auto;
    min-width: 300px;
`

export const TitleComponent = styled.div`
    display: flex;
    align-items: center;
`

export const EditButton = styled(Button)`
    padding-left: 5px;
`

export const HeaderSection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
`

export const TitleSection = styled.div`
    display: flex;
    align-items: center;
    gap: 10px;
`
