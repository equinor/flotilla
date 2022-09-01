import { Autocomplete, Button, Card, Checkbox, Dialog, Typography } from "@equinor/eds-core-react";
import { useState } from "react";
import styled from "styled-components";

interface IProps {
    options: Array<string>;
}

const StyledMissionDialog = styled.div`
    display: flex;
    justify-content: center;
`
const StyledAutoComplete = styled(Card)`
    display: flex;
    justify-content: center;
    padding: 8px;
`

const StyledMissionSection = styled.div`
    margin-left: auto;
    margin-right: 0;
`

export const ScheduleMissionDialog = (props: IProps): JSX.Element => {
    const [isOpen, setIsOpen] = useState<boolean>(false);
    return (
        <>

            <Button onClick={() => { setIsOpen(true) }}>Schedule Mission</Button>
            <StyledMissionDialog>
                <Dialog open={isOpen} isDismissable>
                    <StyledAutoComplete>
                        <Typography variant="h5">Schedule mission</Typography>
                        <Autocomplete
                            options={props.options}
                            label={"Schedule Missions"}
                        />
                        <StyledMissionSection>
                            <Button onClick={() => { setIsOpen(false) }} variant="outlined" color="secondary"> Cancel </Button>
                            <Button onClick={() => { setIsOpen(false) }}> Schedule mission</Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </Dialog>
            </StyledMissionDialog>
        </>
    )
}
