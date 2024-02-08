import { Button, Card, Dialog } from '@equinor/eds-core-react'
import styled from 'styled-components'

export const StyledDict = {
    FormCard: styled(Card)`
        display: flex;
        justify-content: center;
        padding: 8px;
        gap: 25px;
    `,

    ButtonSection: styled.div`
        display: flex;
        margin-left: auto;
        gap: 10px;
    `,

    FormContainer: styled.div`
        display: flex;
        flex-wrap: wrap;
        max-width: 1000px;
        min-width: 270px;
        gap: 10px 20px;
        margin: 1rem;
        @media (min-width: 800px) {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
        }
    `,

    FormItem: styled.div`
        width: 100%;
        height: auto;
        padding: 5px;
        word-break: break-word;
        hyphens: auto;
        min-height: 80px;
    `,

    Dialog: styled(Dialog)`
        display: flex;
        justify-content: space-between;
        padding: 1rem;
        width: 620px;
    `,

    MissionDefinitionPage: styled.div`
        display: flex;
        flex-wrap: wrap;
        justify-content: start;
        flex-direction: column;
        gap: 1rem;
        margin: 2rem;
    `,

    Button: styled(Button)`
        width: 260px;
    `,

    InspectionFrequencyDiv: styled.div`
        padding: 10px;
    `,

    TitleComponent: styled.div`
        display: flex;
        align-items: center;
        height: 36px;
    `,

    EditButton: styled(Button)`
        padding-left: 5px;
    `,

    Card: styled(Card)`
        display: flex;
        padding: 8px;
        height: 110px;
    `,
    HeaderSection: styled.div`
        display: flex;
        flex-direction: column;
        gap: 0.4rem;
    `,

    TitleSection: styled.div`
        display: flex;
        align-items: center;
        gap: 10px;
    `,
}
